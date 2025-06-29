namespace FeedbackSorter.Application.Feedback.Repositories.UserFeedbackReadRepository;

public record FailedToAnalyzeUserFeedbackQueryParams
{
    public UserFeedbackSortBy? SortBy { get; init; }
    public bool SortAscending { get; init; } = true;
}
