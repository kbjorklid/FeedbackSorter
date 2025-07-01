using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Infrastructure;

public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
