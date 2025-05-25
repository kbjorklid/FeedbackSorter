using System;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Core.UnitTests.Builders;

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
        return new FeedbackId(_value);
    }
}
