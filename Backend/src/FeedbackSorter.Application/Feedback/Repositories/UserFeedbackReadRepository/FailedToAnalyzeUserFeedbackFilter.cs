namespace FeedbackSorter.Application.Feedback.Repositories.UserFeedbackReadRepository;

public record FailedToAnalyzeUserFeedbackFilter
{
    public UserFeedbackSortBy? SortBy { get; init; }
    public bool SortAscending { get; init; } = true;
}
