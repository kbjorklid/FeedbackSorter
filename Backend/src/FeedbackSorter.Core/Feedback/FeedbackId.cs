namespace FeedbackSorter.Core.Feedback;

/// <summary>
/// Represents the unique identifier for a user feedback.
/// </summary>
public readonly record struct FeedbackId
{
    public Guid Value { get; }

    private FeedbackId(Guid value)
    {
        Value = value;
    }

    public static FeedbackId New() => new(Guid.NewGuid());

    public static FeedbackId FromGuid(Guid guid) => new(guid);

    public static FeedbackId FromString(string guidString)
    {
        if (!Guid.TryParse(guidString, out Guid guid))
        {
            throw new ArgumentException("Invalid GUID string format.", nameof(guidString));
        }
        return new(guid);
    }
}
