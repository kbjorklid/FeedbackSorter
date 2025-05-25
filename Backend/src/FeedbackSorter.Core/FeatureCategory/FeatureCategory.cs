using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.FeatureCategory;

/// <summary>
/// Represents a category of features in the system.
/// </summary>
public class FeatureCategory : Entity
{
    public FeatureCategoryName Name { get; private set; }
    public Timestamp CreatedAt { get; }

    public FeatureCategory(FeatureCategoryId id, FeatureCategoryName name, ITimeProvider timeProvider)
    {
        Id = id.Value;
        Name = name;
        CreatedAt = new Timestamp(timeProvider);
    }

    public void UpdateName(FeatureCategoryName newName)
    {
        Name = newName;
    }
}
