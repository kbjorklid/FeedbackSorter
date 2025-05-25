namespace FeedbackSorter.Core.UserFeedback;

/// <summary>
/// Represents the text content of user feedback.
/// </summary>
public record struct FeedbackText
{
    public string Value { get; }

    public FeedbackText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Feedback text cannot be null or whitespace.", nameof(value));
        if (value.Length is >= 3 and <= 2000)
            throw new ArgumentException("Feedback text must be between 3 and 2000 characters.", nameof(value));
        Value = value;
    }
}
