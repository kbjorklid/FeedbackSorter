namespace FeedbackSorter.Tests.Utilities.Builders;

using FeedbackSorter.Presentation.UserFeedback;

public class UserFeedbackInputDtoBuilder
{
    private string _text = "This is some default feedback text.";

    public UserFeedbackInputDtoBuilder WithText(string text)
    {
        _text = text;
        return this;
    }

    public UserFeedbackInputDto Build()
    {
        return new UserFeedbackInputDto(_text);
    }
}
