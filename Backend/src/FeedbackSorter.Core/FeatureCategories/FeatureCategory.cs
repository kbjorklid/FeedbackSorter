using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.FeatureCategories;

/// <summary>
/// Represents a category of features in the system.
/// </summary>
public class FeatureCategory(FeatureCategoryId id, FeatureCategoryName name, Timestamp? createdAt = null)
    : Entity<FeatureCategoryId>(id)
{
    public FeatureCategoryName Name { get; private set; } = name;
    public Timestamp CreatedAt { get; } = createdAt ?? new Timestamp();

    public FeatureCategory(FeatureCategoryName name, ITimeProvider timeProvider) :
            this(new FeatureCategoryId(), name, new Timestamp(timeProvider.UtcNow))
    {
    }

    public void UpdateName(FeatureCategoryName newName)
    {
        Name = newName;
    }
}
