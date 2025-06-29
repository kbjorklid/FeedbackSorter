using FeedbackSorter.Core.FeatureCategories;

namespace FeedbackSorter.Core.Feedback;

/// <summary>
/// Represents the result of analyzing user feedback.
/// </summary>
public record FeedbackAnalysisResult(
    FeedbackTitle Title,
    Sentiment Sentiment,
    ISet<FeedbackCategoryType> FeedbackCategories,
    ISet<FeatureCategory> FeatureCategories,
    DateTime AnalyzedAt);
