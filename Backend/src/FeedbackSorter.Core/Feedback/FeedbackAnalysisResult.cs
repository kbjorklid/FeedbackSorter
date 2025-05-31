using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.Feedback;

/// <summary>
/// Represents the result of analyzing user feedback.
/// </summary>
public record FeedbackAnalysisResult
{
    public FeedbackTitle Title { get; }
    public Sentiment Sentiment { get; }
    public ISet<FeedbackCategoryType> FeedbackCategories { get; }
    public ISet<FeatureCategory> FeatureCategories { get; }
    public Timestamp AnalyzedAt { get; }

    public FeedbackAnalysisResult(FeedbackTitle title, Sentiment sentiment, ISet<FeedbackCategoryType> feedbackCategories, ISet<FeatureCategory> featureCategories, Timestamp analyzedAt)
    {
        Title = title;
        Sentiment = sentiment;
        FeedbackCategories = feedbackCategories;
        FeatureCategories = featureCategories;
        AnalyzedAt = analyzedAt;
    }
}
