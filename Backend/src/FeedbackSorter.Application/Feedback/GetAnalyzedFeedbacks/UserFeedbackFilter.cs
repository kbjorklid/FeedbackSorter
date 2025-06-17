using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.GetAnalyzedFeedbacks;

public record UserFeedbackFilter
{
    public IEnumerable<FeedbackCategoryType>? FeedbackCategories { get; init; }
    public IEnumerable<FeatureCategoryId>? FeatureCategoryIds { get; init; }
    public AnalysisStatus? AnalysisStatus { get; init; }
    public UserFeedbackSortBy? SortBy { get; init; }
    public bool SortAscending { get; init; } = true;
}
