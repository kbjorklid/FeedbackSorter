using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Core.Feedback;
namespace FeedbackSorter.Application.LLM;

public interface ILlmFeedbackAnalyzer
{
    Task<LlmAnalysisResult> AnalyzeFeedback(
        FeedbackText feedbackText,
        IEnumerable<FeatureCategoryReadModel> existingFeatureCategories);
}
