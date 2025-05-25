using FeedbackSorter.Core.FeatureCategory;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.Feedback;

/// <summary>
/// Represents the result of analyzing user feedback.
/// </summary>
public record FeedbackAnalysisResult
{
    public FeedbackTitle Title { get; }
    public Sentiment Sentiment { get; }
    public IReadOnlyList<FeedbackCategoryType> FeedbackCategories { get; }
    public IReadOnlyList<FeatureCategoryId> FeatureCategoryIds { get; }
    public Timestamp AnalyzedAt { get; }

    public FeedbackAnalysisResult(FeedbackTitle title, Sentiment sentiment, IReadOnlyList<FeedbackCategoryType> feedbackCategories, IReadOnlyList<FeatureCategoryId> featureCategoryIds, Timestamp analyzedAt)
    {
        Title = title;
        Sentiment = sentiment;
        FeedbackCategories = feedbackCategories;
        FeatureCategoryIds = featureCategoryIds;
        AnalyzedAt = analyzedAt;
    }
}
