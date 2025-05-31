using FeedbackSorter.Application.FeatureCategories.Queries;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.LLM;

public interface ILLMFeedbackAnalyzer
{
    Task<Result<LLMAnalysisResult>> AnalyzeFeedback(
        FeedbackText feedbackText,
        IEnumerable<FeatureCategoryReadModel> existingFeatureCategories,
        IEnumerable<Sentiment> sentimentChoices,
        IEnumerable<FeedbackCategoryType> feedbackCategoryChoices);
}
