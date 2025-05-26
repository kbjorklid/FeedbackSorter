namespace FeedbackSorter.Application.UserFeedback.Queries;

public record FailedUserFeedbackFilter
{
    public string? SortBy { get; init; }
    public bool SortAscending { get; init; } = true;
}
