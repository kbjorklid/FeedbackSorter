using FeedbackSorter.Core;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;

public class UserFeedbackQuery
{
    public IReadOnlySet<AnalysisStatus>? AnalysisStatuses { get; init; }
    public UserFeedbackSortBy SortBy { get; init; } = UserFeedbackSortBy.SubmittedAt;

    public SortOrder Order { get; init; } = SortOrder.Asc;
    public int? MaxResults { get; init; }
}
