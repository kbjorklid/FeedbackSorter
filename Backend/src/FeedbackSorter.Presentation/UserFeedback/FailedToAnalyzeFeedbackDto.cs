namespace FeedbackSorter.Presentation.UserFeedback;

public class FailedToAnalyzeFeedbackDto
{
    public required Guid Id { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public required int RetryCount { get; init; }
    public required string FullFeedbackText { get; init; }
}
