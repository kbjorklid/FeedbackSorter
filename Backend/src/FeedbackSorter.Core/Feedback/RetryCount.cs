namespace FeedbackSorter.Core.Feedback;

/// <summary>
/// Represents the number of times a feedback analysis has been retried.
/// </summary>
public readonly record struct RetryCount(int Value)
{
    public RetryCount Increment() => new RetryCount(Value + 1);
}
