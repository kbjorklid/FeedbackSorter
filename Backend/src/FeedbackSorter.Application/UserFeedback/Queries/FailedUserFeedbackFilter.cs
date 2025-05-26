namespace FeedbackSorter.Application.UserFeedback.Queries;

public record FailedToAnalyzeUserFeedbackFilter
{
    public string? SortBy { get; init; }
    public bool SortAscending { get; init; } = true;
}
