using FeedbackSorter.Application.Feedback.Repositories;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.Queries.GetAnalyzedFeedbacks;

public record AnalyzedFeedbackQueryParams
{
    public IEnumerable<FeedbackCategoryType>? FeedbackCategories { get; init; }
    public IEnumerable<FeatureCategoryId>? FeatureCategoryIds { get; init; }
    public UserFeedbackSortBy? SortBy { get; init; }
    public bool SortAscending { get; init; } = true;
}
