using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.FeatureCategories;

/// <summary>
/// Represents a category of features in the system.
/// </summary>
public class FeatureCategory(FeatureCategoryId id, FeatureCategoryName name, DateTime? createdAt = null)
    : Entity<FeatureCategoryId>(id)
{
    public FeatureCategoryName Name { get; private set; } = name;
    public DateTime CreatedAt { get; } = createdAt ?? DateTime.UtcNow;

    public FeatureCategory(FeatureCategoryName name, ITimeProvider timeProvider) :
            this(new FeatureCategoryId(), name, timeProvider.UtcNow)
    {
    }

    public void UpdateName(FeatureCategoryName newName)
    {
        Name = newName;
    }
}
