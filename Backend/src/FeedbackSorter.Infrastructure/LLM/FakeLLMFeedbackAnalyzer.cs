using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Infrastructure.LLM;

public class FakeLLMFeedbackAnalyzer : ILLMFeedbackAnalyzer
{
    private static int _callCount = 0;

    public Task<Result<FeedbackAnalysisResult>> AnalyzeFeedback(
        FeedbackText feedbackText,
        IEnumerable<FeatureCategory> existingFeatureCategories,
        IEnumerable<Sentiment> sentimentChoices,
        IEnumerable<FeedbackCategoryType> feedbackCategoryChoices)
    {
        _callCount++;

        if (_callCount % 2 != 0) // Odd calls are successful
        {
            var bogusTitle = new FeedbackTitle($"Bogus Title {_callCount}");
            Sentiment bogusSentiment = Sentiment.Positive; // Can cycle through sentiments if needed
            var bogusFeedbackCategories = new List<FeedbackCategoryType> { FeedbackCategoryType.FeatureRequest };
            var bogusFeatureCategoryIds = new List<FeatureCategoryId> { new FeatureCategoryId(Guid.NewGuid()) };

            var successResult = new FeedbackAnalysisResult(
                title: bogusTitle,
                sentiment: bogusSentiment,
                feedbackCategories: bogusFeedbackCategories,
                featureCategoryIds: bogusFeatureCategoryIds,
                analyzedAt: new Timestamp(DateTime.UtcNow)
            );
            return Task.FromResult(Result<FeedbackAnalysisResult>.Success(successResult));
        }
        else // Even calls are failures
        {
            var failureDetails = new AnalysisFailureDetails(
                FailureReason.LlmError,
                "Simulated LLM failure: Could not analyze feedback due to an internal error.",
                new Timestamp(DateTime.UtcNow),
                _callCount
            );
            return Task.FromResult(Result<FeedbackAnalysisResult>.Failure(failureDetails.Message ?? "Unknown LLM failure"));
        }
    }
}
