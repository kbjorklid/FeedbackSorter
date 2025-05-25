using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Core.UnitTests.Builders;

public class FeedbackTextBuilder
{
    private string _value = "This is a default feedback text for testing purposes.";

    public FeedbackTextBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public FeedbackText Build()
    {
        return new FeedbackText(_value);
    }
}
