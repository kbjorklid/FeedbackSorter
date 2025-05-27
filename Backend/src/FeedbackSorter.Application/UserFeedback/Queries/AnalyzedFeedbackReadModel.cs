using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.UserFeedback.Queries;

public record AnalyzedFeedbackReadModel
{
    public required FeedbackId Id { get; init; }
    public required string Title { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public required IEnumerable<FeedbackCategoryType> FeedbackCategories { get; init; }
    public required IEnumerable<FeatureCategoryId> FeatureCategoryIds { get; init; }
    public required Sentiment Sentiment { get; init; }
    public required string FullFeedbackText { get; init; }
}
