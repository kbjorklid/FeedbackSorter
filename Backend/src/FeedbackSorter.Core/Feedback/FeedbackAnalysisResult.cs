using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.Feedback;

/// <summary>
/// Represents the result of analyzing user feedback.
/// </summary>
public record FeedbackAnalysisResult(
    FeedbackTitle Title,
    Sentiment Sentiment,
    ISet<FeedbackCategoryType> FeedbackCategories,
    ISet<FeatureCategory> FeatureCategories,
    Timestamp AnalyzedAt);
