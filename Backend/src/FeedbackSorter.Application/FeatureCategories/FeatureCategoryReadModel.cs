using FeedbackSorter.Core.FeatureCategories;

namespace FeedbackSorter.Application.FeatureCategories;

public record FeatureCategoryReadModel
{
    public FeatureCategoryId Id { get; init; }
    public FeatureCategoryName Name { get; init; }

    public FeatureCategoryReadModel(FeatureCategoryId id, FeatureCategoryName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Id = id;
        Name = name;
    }

    public FeatureCategoryReadModel(FeatureCategory featureCategory) : this(featureCategory.Id, featureCategory.Name)
    {
    }
}
