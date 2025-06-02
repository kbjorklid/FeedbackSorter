namespace FeedbackSorter.Application.Feedback.Queries;

public record FailedToAnalyzeUserFeedbackFilter
{
    public UserFeedbackSortBy? SortBy { get; init; }
    public bool SortAscending { get; init; } = true;
}
