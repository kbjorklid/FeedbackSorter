using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.Feedback;

/// <summary>
/// Represents the text content of user feedback.
/// </summary>
public record struct FeedbackText
{
    public string Value { get; }

    public FeedbackText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw DomainValidationException.ForArgument(
                "Feedback text cannot be null or whitespace.", nameof(value));
        }
        if (value.Length is < 3 or > 2000)
        {
            throw DomainValidationException.ForArgument(
                "Feedback text must be between 3 and 2000 characters.", nameof(value));
        }

        Value = value;
    }
}
