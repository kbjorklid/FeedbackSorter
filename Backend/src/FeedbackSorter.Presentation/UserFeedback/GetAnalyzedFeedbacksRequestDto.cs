using FeedbackSorter.Application.Feedback.Query;
using FeedbackSorter.Application.Feedback.Repositories;
using FeedbackSorter.Core;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Presentation.UserFeedback;

public class GetAnalyzedFeedbacksRequestDto : PagedRequestBaseDto
{
    public UserFeedbackSortBy SortBy { get; set; } = UserFeedbackSortBy.SubmittedAt;
    public SortOrder SortOrder { get; set; } = SortOrder.Desc;
    public FeedbackCategoryType? FeedbackCategory { get; set; }
    public string? FeatureCategoryName { get; set; }
    
    public Sentiment? Sentiment { get; set; }

    public GetAnalyzedFeedbacksQuery ToQuery()
    {
        return new GetAnalyzedFeedbacksQuery(
            PageNumber,
            PageSize,
            SortBy,
            SortOrder,
            FeedbackCategory == null ? null : [FeedbackCategory.Value],
            FeatureCategoryName == null ? null : [FeatureCategoryName],
            Sentiment
        );
    }
}
