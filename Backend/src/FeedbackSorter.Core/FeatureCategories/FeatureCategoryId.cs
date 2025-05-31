namespace FeedbackSorter.Core.FeatureCategories;

/// <summary>
/// Represents the unique identifier for a feature category.
/// </summary>
public readonly record struct FeatureCategoryId
{

    public Guid Value { get; }

    public FeatureCategoryId(Guid value)
    {
        this.Value = value;
    }

    public FeatureCategoryId() : this(Guid.NewGuid())
    {
    }
}
