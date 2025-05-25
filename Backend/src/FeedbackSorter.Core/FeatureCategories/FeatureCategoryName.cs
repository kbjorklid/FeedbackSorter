namespace FeedbackSorter.Core.FeatureCategories;

/// <summary>
/// Represents the name of a feature category.
/// </summary>
public record class FeatureCategoryName
{
    public string Value { get; }

    public FeatureCategoryName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Feature category name cannot be null or whitespace.", nameof(value));
        }

        Value = value;
    }
}
