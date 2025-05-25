namespace FeedbackSorter.Core.Feedback;

/// <summary>
/// Represents the title of user feedback.
/// </summary>
public record struct FeedbackTitle
{
    public string Value { get; }

    public FeedbackTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Feedback title cannot be null or whitespace.", nameof(value));
        if (value.Length > 50)
            throw new ArgumentException("Feedback title cannot be longer than 50 characters.", nameof(value));
        Value = value;
    }
}
