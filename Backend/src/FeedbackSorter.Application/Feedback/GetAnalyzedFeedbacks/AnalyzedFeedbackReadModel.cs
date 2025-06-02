using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.GetAnalyzedFeedbacks;

public record AnalyzedFeedbackReadModel<TFeatureCategoryRepresentation>
{
    public required FeedbackId Id { get; init; }
    public required string Title { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public required ISet<FeedbackCategoryType> FeedbackCategories { get; init; }
    public required ISet<TFeatureCategoryRepresentation> FeatureCategories { get; init; }
    public required Sentiment Sentiment { get; init; }
    public required string FullFeedbackText { get; init; }

    internal AnalyzedFeedbackReadModel<TTargetCategory> Map<TTargetCategory>(Func<TFeatureCategoryRepresentation, TTargetCategory> categoryMapFunction)
    {
        return new AnalyzedFeedbackReadModel<TTargetCategory>
        {
            Id = this.Id,
            Title = this.Title,
            SubmittedAt = this.SubmittedAt,
            FeedbackCategories = this.FeedbackCategories,
            FeatureCategories = this.FeatureCategories.Select(categoryMapFunction).ToHashSet(),
            Sentiment = this.Sentiment,
            FullFeedbackText = this.FullFeedbackText,
        };
    }
}
