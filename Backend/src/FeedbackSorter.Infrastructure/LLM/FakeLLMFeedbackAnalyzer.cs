using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Infrastructure.LLM;

public class FakeLLMFeedbackAnalyzer : ILlmFeedbackAnalyzer
{
    private static int _callCount = 0;

    public Task<Result<LlmAnalysisResult>> AnalyzeFeedback(
        FeedbackText feedbackText,
        IEnumerable<FeatureCategoryReadModel> existingFeatureCategories,
        IEnumerable<Sentiment> sentimentChoices,
        IEnumerable<FeedbackCategoryType> feedbackCategoryChoices)
    {
        _callCount++;

        if (_callCount % 2 != 0) // Odd calls are successful
        {
            var bogusTitle = new FeedbackTitle($"Bogus Title {_callCount}");
            Sentiment bogusSentiment = Sentiment.Positive; // Can cycle through sentiments if needed
            var bogusFeedbackCategories = new HashSet<FeedbackCategoryType> { FeedbackCategoryType.FeatureRequest };
            var bogusFeatureCategoryNames = new HashSet<string> { "Some Feature Category" };

            var successResult = new LlmAnalysisResult()
            {
                Title = bogusTitle,
                Sentiment = bogusSentiment,
                FeedbackCategories = bogusFeedbackCategories,
                FeatureCategoryNames = bogusFeatureCategoryNames,
                AnalyzedAt = new Timestamp(DateTime.UtcNow)
            };
            return Task.FromResult(Result<LlmAnalysisResult>.Success(successResult));
        }
        else // Even calls are failures
        {
            var failureDetails = new AnalysisFailureDetails(
                FailureReason.LlmError,
                "Simulated LLM failure: Could not analyze feedback due to an internal error.",
                new Timestamp(DateTime.UtcNow),
                _callCount
            );
            return Task.FromResult(Result<LlmAnalysisResult>.Failure(failureDetails.Message ?? "Unknown LLM failure"));
        }
    }
}
