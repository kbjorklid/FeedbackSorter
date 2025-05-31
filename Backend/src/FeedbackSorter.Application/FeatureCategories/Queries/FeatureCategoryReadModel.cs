using FeedbackSorter.Core.FeatureCategories;

namespace FeedbackSorter.Application.FeatureCategories.Queries;

public record FeatureCategoryReadModel
{
    public FeatureCategoryId Id { get; init; }
    public FeatureCategoryName Name { get; init; }

    public FeatureCategoryReadModel(FeatureCategoryId id, FeatureCategoryName name)
    {
        Id = id;
        Name = name;
    }

    public FeatureCategoryReadModel(FeatureCategory featureCategory)
    {
        Id = featureCategory.Id;
        Name = featureCategory.Name;
    }
}
