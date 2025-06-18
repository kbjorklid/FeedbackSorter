using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Tests.Utilities.Builders;

public class FeedbackTitleBuilder
{
    private string _value = "Default Feedback Title";

    public FeedbackTitleBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public FeedbackTitle Build()
    {
        return new FeedbackTitle(_value);
    }
}
