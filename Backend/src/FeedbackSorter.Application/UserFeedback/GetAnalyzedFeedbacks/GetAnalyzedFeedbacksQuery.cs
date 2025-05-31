using FeedbackSorter.Core;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.UserFeedback.GetAnalyzedFeedbacks;

public record GetAnalyzedFeedbacksQuery(
    int PageNumber,
    int PageSize,
    UserFeedbackSortBy SortBy,
    SortOrder SortOrder,
    IEnumerable<FeedbackCategoryType>? FeedbackCategories,
    IEnumerable<string>? FeatureCategoryNames
);
