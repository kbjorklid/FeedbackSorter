using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.Repositories;

public record AnalyzedFeedbackQueryParams
{
    public IEnumerable<FeedbackCategoryType>? FeedbackCategories { get; init; }
    public IEnumerable<FeatureCategoryName>? FeatureCategoryNames { get; init; }
    public Sentiment? Sentiment { get; init; }
    public UserFeedbackSortBy? SortBy { get; init; }
    public bool SortAscending { get; init; } = true;
    
}
