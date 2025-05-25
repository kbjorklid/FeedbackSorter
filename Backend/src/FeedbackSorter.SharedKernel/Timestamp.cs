namespace FeedbackSorter.SharedKernel;

/// <summary>
/// Represents a specific point in time.
/// </summary>
public readonly record struct Timestamp(DateTime Value)
{
    public Timestamp(ITimeProvider timeProvider) : this(timeProvider.UtcNow)
    {
    }
};
