using FeedbackSorter.Application.UserFeedback.Queries;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Presentation.UserFeedback;

public class GetAnalyzedFeedbacksRequestDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public UserFeedbackSortBy SortBy { get; set; } = UserFeedbackSortBy.SubmittedAt;
    public SortOrder SortOrder { get; set; } = SortOrder.Desc;
    public IEnumerable<FeedbackCategoryType>? FeedbackCategories { get; set; }
    public IEnumerable<string>? FeatureCategoryNames { get; set; }

    public GetAnalyzedFeedbacksQuery ToQuery()
    {
        return new GetAnalyzedFeedbacksQuery(
            PageNumber,
            PageSize,
            SortBy,
            SortOrder,
            FeedbackCategories,
            FeatureCategoryNames
        );
    }
}
