using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Tests.Utilities.Builders;

public class FeedbackIdBuilder
{
    private Guid _value = Guid.NewGuid();

    public FeedbackIdBuilder WithValue(Guid value)
    {
        _value = value;
        return this;
    }

    public FeedbackId Build()
    {
        return FeedbackId.FromGuid(_value);
    }
}
