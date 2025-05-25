using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.Feedback;

/// <summary>
/// Represents details about a failure that occurred during feedback analysis.
/// </summary>
public record AnalysisFailureDetails
{
    public FailureReason Reason { get; }
    public string? Message { get; }
    public Timestamp OccurredAt { get; }
    public int AttemptNumber { get; }

    public AnalysisFailureDetails(FailureReason reason, string? message, Timestamp occurredAt, int attemptNumber)
    {
        Reason = reason;
        Message = message;
        OccurredAt = occurredAt;
        AttemptNumber = attemptNumber;
    }
}
