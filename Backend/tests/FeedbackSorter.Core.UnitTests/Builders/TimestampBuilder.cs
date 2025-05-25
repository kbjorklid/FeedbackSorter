using FeedbackSorter.SharedKernel;
using System;

namespace FeedbackSorter.Core.UnitTests.Builders;

public class TimestampBuilder
{
    private DateTime _value = DateTime.UtcNow;

    public TimestampBuilder WithValue(DateTime value)
    {
        _value = value;
        return this;
    }

    public Timestamp Build()
    {
        return new Timestamp(_value);
    }
}
