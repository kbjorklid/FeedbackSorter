using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.LLM;

public interface ILlmFeedbackAnalyzer
{
    Task<Result<LlmAnalysisResult>> AnalyzeFeedback(
        FeedbackText feedbackText,
        IEnumerable<FeatureCategoryReadModel> existingFeatureCategories,
        IEnumerable<Sentiment> sentimentChoices,
        IEnumerable<FeedbackCategoryType> feedbackCategoryChoices);
}
