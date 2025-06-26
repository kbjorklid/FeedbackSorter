using FeedbackSorter.Core.FeatureCategories;

namespace FeedbackSorter.Application.FeatureCategories.Repositories;

public record FeatureCategoryReadModel
{
    public FeatureCategoryId Id { get; }
    public FeatureCategoryName Name { get; }

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
