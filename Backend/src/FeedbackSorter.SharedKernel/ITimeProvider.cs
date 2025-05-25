namespace FeedbackSorter.SharedKernel;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
}