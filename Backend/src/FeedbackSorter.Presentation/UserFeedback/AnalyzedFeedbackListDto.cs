using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Presentation.UserFeedback;

public class AnalyzedFeedbackListDto : PagedResult<AnalyzedFeedbackItemDto>
{
    public AnalyzedFeedbackListDto(IEnumerable<AnalyzedFeedbackItemDto> items, int pageNumber, int pageSize, int totalCount)
        : base(items, pageNumber, pageSize, totalCount)
    {
    }
}
