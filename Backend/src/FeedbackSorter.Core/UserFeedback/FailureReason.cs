namespace FeedbackSorter.Core.UserFeedback;

/// <summary>
/// Represents the reason why feedback analysis failed.
/// </summary>
public enum FailureReason
{
    LlmError,
    LlmUnableToProcess,
    Unknown
}
