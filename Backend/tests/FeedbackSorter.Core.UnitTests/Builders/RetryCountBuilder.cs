using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Core.UnitTests.Builders;

public class RetryCountBuilder
{
    private int _value = 0;

    public RetryCountBuilder WithValue(int value)
    {
        _value = value;
        return this;
    }

    public RetryCount Build()
    {
        return new RetryCount(_value);
    }
}
