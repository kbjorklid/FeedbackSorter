// Path: Backend/src/FeedbackSorter.Infrastructure/Persistence/Mappers/FeatureCategoryMapper.cs
using FeedbackSorter.Application.FeatureCategories.Repositories; // Added for FeatureCategoryReadModel
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Infrastructure.Persistence.Models;

namespace FeedbackSorter.Infrastructure.Persistence.Mappers;

public static class FeatureCategoryMapper
{
    public static FeatureCategoryDb ToDbEntity(FeatureCategory domainEntity)
    {
        return new FeatureCategoryDb
        {
            Id = domainEntity.Id.Value,
            Name = domainEntity.Name.Value,
            CreatedAt = domainEntity.CreatedAt
        };
    }

    public static FeatureCategory ToDomainEntity(FeatureCategoryDb dbEntity)
    {
        return new FeatureCategory(
            new FeatureCategoryId(dbEntity.Id),
            new FeatureCategoryName(dbEntity.Name),
            dbEntity.CreatedAt
        );
    }

    public static FeatureCategoryReadModel ToReadModel(FeatureCategoryDb dbEntity)
    {
        return new FeatureCategoryReadModel(
            new FeatureCategoryId(dbEntity.Id),
            new FeatureCategoryName(dbEntity.Name)
        );
    }
}
