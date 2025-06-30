using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;
using Microsoft.SemanticKernel;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace FeedbackSorter.Infrastructure.LLM;

public class SemanticKernelLLMAnalyzer : ILlmFeedbackAnalyzer
{
    private readonly Kernel _kernel;

    public SemanticKernelLLMAnalyzer(IConfiguration configuration)
    {
        string? apiKey = configuration["LLM:ApiKey"];
        string? endpointUrl = configuration["LLM:EndpointUrl"];
        string? modelId = configuration["LLM:ModelId"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpointUrl) || string.IsNullOrEmpty(modelId))
            throw new InvalidOperationException("LLM settings not configured properly.");
        
        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: apiKey,
            endpoint: new Uri(endpointUrl));
        _kernel = kernelBuilder.Build();
    }
    
    public async Task<LlmAnalysisResult> AnalyzeFeedback(FeedbackText feedbackText,
        IEnumerable<FeatureCategoryReadModel> existingFeatureCategories)
    {
        string prompt = BuildPrompt(feedbackText, existingFeatureCategories);
        
        try
        {
            FunctionResult result = await _kernel.InvokePromptAsync(prompt);
            return HandleLlmResult(result);
        }
        catch (JsonException ex)
        {
            return LlmAnalysisResult.ForFailure(DateTime.UtcNow,
                new LlmAnalysisFailure
                {
                    Error = $"Failed to parse LLM response as JSON: {ex.Message}",
                    Reason = FailureReason.LlmOutputFormatInvalid
                });
        }
        catch (Exception ex)
        {
            return LlmAnalysisResult.ForFailure(DateTime.UtcNow,
                new LlmAnalysisFailure
                {
                    Error = $"An unexpected error occurred during LLM analysis: {ex.Message}",
                    Reason = FailureReason.LlmError
                });
        }
    }

    private static LlmAnalysisResult HandleLlmResult(FunctionResult result)
    {
        string? jsonResponse = result.GetValue<string>();

        if (jsonResponse == null)
        {
            return LlmAnalysisResult.ForFailure(DateTime.UtcNow,
                new LlmAnalysisFailure { Error = "LLM returned null", Reason = FailureReason.LlmError });
        }

        jsonResponse = ExtractJsonContent(jsonResponse);

        LlmOutput? llmOutput = JsonSerializer.Deserialize<LlmOutput>(jsonResponse,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (llmOutput == null)
        {
            return LlmAnalysisResult.ForFailure(DateTime.UtcNow,
                new LlmAnalysisFailure
                {
                    Error = "LLM returned empty or unparseable JSON.",
                    Reason = FailureReason.LlmOutputFormatInvalid
                });
        }

        // Validate sentiment
        if (!Enum.TryParse(llmOutput.Sentiment, true, out Sentiment sentiment))
        {
            return LlmAnalysisResult.ForFailure(DateTime.UtcNow,
                new LlmAnalysisFailure
                {
                    Error = $"Invalid sentiment returned by LLM: {llmOutput.Sentiment}",
                    Reason = FailureReason.LlmOutputFormatInvalid
                });
        }

        // Validate feedback categories
        var feedbackCategories = new HashSet<FeedbackCategoryType>();
        foreach (var categoryString in llmOutput.FeedbackCategories)
        {
            if (Enum.TryParse(categoryString, true, out FeedbackCategoryType categoryType))
            {
                feedbackCategories.Add(categoryType);
            }
        }

        return LlmAnalysisResult.ForSuccess(DateTime.UtcNow,
            new LlmAnalysisSuccess
            {
                Title = new FeedbackTitle(llmOutput.Title),
                Sentiment = sentiment,
                FeedbackCategories = feedbackCategories,
                FeatureCategoryNames = new HashSet<string>(llmOutput.FeatureCategoryNames)
            });
    }

    private static string BuildPrompt(FeedbackText feedbackText, IEnumerable<FeatureCategoryReadModel> existingFeatureCategories)
    {
        string existingCategories = string.Join(", ", existingFeatureCategories.Select(c => $"\"{c.Name}\""));

        const string userFeedbackExample = """
                                           ```
                                           Hi,
                                           I cannot recover my password. On the login page, when I click the 'password recovery'
                                           link, the site says it has sent me an email, but I never received one.
                                           Let me know if you need any further details.
                                           ```
                                           """;
        const string jsonExample = """
                                   ```json
                                   {
                                      "Title": "Recover Password functionality is broken",
                                      "Sentiment": "Neutral",
                                      "FeedbackCategories": ["Bug"],
                                      "FeatureCategoryNames": ["Login Page", "Password Recovery"]
                                   }
                                   ```
                                   """;
        string prompt = $"""
                         You are an AI assistant that analyzes user feedback of a product.
                         Analyze the following user feedback and categorize it based on the provided feature categories.
                         Return the analysis in a JSON format.

                         User Feedback:,
                         {feedbackText.Value}

                         Existing Feature Categories: [{existingCategories}],

                         Expected JSON Output Format:
                         {JsonTemplate()}

                         - Ensure the JSON is valid and strictly adheres to the specified format.
                         - If you cannot determine a suitable Feedback Category, use "Other".
                         - Think what you might call the feature the feedback is about. Either pick existing feature
                           categories based on the provided list of existing feature categories, or create new ones if
                           no suitable categories exist. The Feature category names should be succinct, 1-4 words. 
                         - Do not include any other text or explanation outside the JSON block.
                         - Allowed values for 'Sentiment' are: "

                         Example:
                         Given this feedback:
                         {userFeedbackExample}
                         The analysis outcome might look something like this:
                         {jsonExample}
                         """;
        return prompt;
    }

    private static string JsonTemplate()
    {
        string[] sentimentStrings = Enum.GetNames(typeof(Sentiment));
        string sentiments = string.Join("|", sentimentStrings);
        string[] feedbackCategoryStrings = Enum.GetNames(typeof(FeedbackCategoryType));
        string categories = string.Join(", ", feedbackCategoryStrings.Select(s => $"\"{s}\""));

        return $$"""
                 ```json
                 {
                    "Title": "Summarized title of the feedback",
                    "Sentiment": "{{sentiments}}",
                    "FeedbackCategories": [{{categories}}],
                    "FeatureCategoryNames": ["Some Feature", "Another Feature"]
                 }
                 ```
                 """;
    }


    private class LlmOutput
    {
        public string Title { get; set; } = string.Empty;
        public string Sentiment { get; set; } = string.Empty;
        public List<string> FeedbackCategories { get; set; } = new List<string>();
        public List<string> FeatureCategoryNames { get; set; } = new List<string>();
    }

    private static string ExtractJsonContent(string llmResponse)
    {
        // Return immediately if the input is null or empty.
        if (string.IsNullOrWhiteSpace(llmResponse))
        {
            return llmResponse;
        }

        const string jsonFence = "```json";
        const string genericFence = "```";

        // --- Scenario 1: Find a Markdown code block ---

        // Find the starting position of the first code fence.
        // Prioritize "```json" but fall back to "```".
        int startIndex = llmResponse.IndexOf(jsonFence, StringComparison.Ordinal);
        int startFenceLength = jsonFence.Length;

        if (startIndex == -1)
        {
            startIndex = llmResponse.IndexOf(genericFence, StringComparison.Ordinal);
            startFenceLength = genericFence.Length;
        }

        // If a starting fence is found...
        if (startIndex != -1)
        {
            // Find the closing fence that comes *after* the opening one.
            int endIndex = llmResponse.IndexOf(genericFence, startIndex + startFenceLength, StringComparison.Ordinal);

            if (endIndex != -1)
            {
                // Extract the content between the fences.
                int contentStartIndex = startIndex + startFenceLength;
                string extractedContent = llmResponse.Substring(contentStartIndex, endIndex - contentStartIndex);

                // Trim whitespace and newlines from the extracted JSON block.
                return extractedContent.Trim();
            }
        }

        // --- Scenario 2: Find the first and last curly braces (fallback) ---

        int firstBraceIndex = llmResponse.IndexOf('{');
        int lastBraceIndex = llmResponse.LastIndexOf('}');

        // Check if both braces are found and in the correct order.
        if (firstBraceIndex != -1 && lastBraceIndex > firstBraceIndex)
        {
            // Extract the substring from the first brace to the last brace (inclusive).
            return llmResponse.Substring(firstBraceIndex, lastBraceIndex - firstBraceIndex + 1);
        }

        // If no patterns match, return the original string as a final fallback.
        return llmResponse;
    }
}
