using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.LLM;

public interface ILLMFeedbackAnalyzer
{
    Task<Result<FeedbackAnalysisResult>> AnalyzeFeedback(
        FeedbackText feedbackText,
        IEnumerable<FeatureCategory> existingFeatureCategories,
        IEnumerable<Sentiment> sentimentChoices,
        IEnumerable<FeedbackCategoryType> feedbackCategoryChoices);
}
