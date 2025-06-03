using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Infrastructure.LLM;

public class FakeLLMFeedbackAnalyzer(ILogger<FakeLLMFeedbackAnalyzer> logger) : ILlmFeedbackAnalyzer
{
    private static int _callCount = 0;

    public Task<LlmAnalysisResult> AnalyzeFeedback(
        FeedbackText feedbackText,
        IEnumerable<FeatureCategoryReadModel> existingFeatureCategories)
    {
        logger.LogDebug("Entering {MethodName} with feedbackText: {FeedbackText}", nameof(AnalyzeFeedback), feedbackText);
        _callCount++;

        if (_callCount % 2 != 0) // Odd calls are successful
        {
            var bogusTitle = new FeedbackTitle($"Bogus Title {_callCount}");
            Sentiment bogusSentiment = Sentiment.Positive; // Can cycle through sentiments if needed
            var bogusFeedbackCategories = new HashSet<FeedbackCategoryType> { FeedbackCategoryType.FeatureRequest };
            var bogusFeatureCategoryNames = new HashSet<string> { "Some Feature Category" };

            var successResult = new LlmAnalysisSuccess()
            {
                Title = bogusTitle,
                Sentiment = bogusSentiment,
                FeedbackCategories = bogusFeedbackCategories,
                FeatureCategoryNames = bogusFeatureCategoryNames,
            };
            var result = LlmAnalysisResult.ForSuccess(new Timestamp(DateTime.UtcNow), successResult);
            return Task.FromResult(result);
        }
        else // Even calls are failures
        {
            var failureResult = new LlmAnalysisFailure()
            {
                Error = "Simulated LLM failure: Could not analyze feedback due to an internal error.",
                Reason = FailureReason.LlmError
            };
            var result = LlmAnalysisResult.ForFailure(new Timestamp(DateTime.UtcNow), failureResult);
            return Task.FromResult(result);
        }
    }
}
