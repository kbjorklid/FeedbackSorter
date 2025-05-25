using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Core.FeatureCategories;
using System.Collections.Generic;

namespace FeedbackSorter.Application.UserFeedback.Queries;

public record UserFeedbackFilter
{
    public IEnumerable<FeedbackCategoryType>? FeedbackCategories { get; init; }
    public IEnumerable<FeatureCategoryId>? FeatureCategoryIds { get; init; }
    public AnalysisStatus? AnalysisStatus { get; init; }
    public string? SortBy { get; init; }
    public bool SortAscending { get; init; } = true;
}
