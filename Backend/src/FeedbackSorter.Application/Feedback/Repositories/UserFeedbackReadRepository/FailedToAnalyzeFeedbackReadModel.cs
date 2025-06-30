using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.Repositories.UserFeedbackReadRepository;

public record FailedToAnalyzeFeedbackReadModel
{
    public required FeedbackId Id { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public required int RetryCount { get; init; }
    public required string FullFeedbackText { get; init; }
}
