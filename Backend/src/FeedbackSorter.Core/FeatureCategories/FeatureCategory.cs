using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.FeatureCategories;

/// <summary>
/// Represents a category of features in the system.
/// </summary>
public class FeatureCategory : Entity<FeatureCategoryId>
{
    public FeatureCategoryName Name { get; private set; }
    public Timestamp CreatedAt { get; }

    public FeatureCategory(FeatureCategoryId id, FeatureCategoryName name, Timestamp createdAt) : base(id)
    {
        Name = name;
        CreatedAt = createdAt;
    }

    public FeatureCategory(FeatureCategoryName name, ITimeProvider timeProvider) :
            this(new FeatureCategoryId(), name, new Timestamp(timeProvider.UtcNow))
    {
    }

    public void UpdateName(FeatureCategoryName newName)
    {
        Name = newName;
    }
}
