using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.SharedKernel;
using CoreFeatureCategory = FeedbackSorter.Core.FeatureCategories.FeatureCategory;

namespace FeedbackSorter.Infrastructure.FeatureCategories;

public class InMemoryFeatureCategoryRepository : IFeatureCategoryRepository, IFeatureCategoryReadRepository
{
    private readonly List<CoreFeatureCategory> _featureCategories;

    public InMemoryFeatureCategoryRepository(List<CoreFeatureCategory> featureCategories)
    {
        _featureCategories = featureCategories;
    }

    public InMemoryFeatureCategoryRepository()
    {
        _featureCategories = [];
    }

    public Task<Result<CoreFeatureCategory>> GetByIdAsync(FeatureCategoryId id)
    {
        CoreFeatureCategory? featureCategory = _featureCategories.FirstOrDefault(fc => fc.Id == id);
        return Task.FromResult(featureCategory != null
            ? Result<CoreFeatureCategory>.Success(featureCategory)
            : Result<CoreFeatureCategory>.Failure("FeatureCategory not found."));
    }

    public Task<FeatureCategory> AddAsync(CoreFeatureCategory featureCategory)
    {
        _featureCategories.Add(featureCategory);
        return Task.FromResult(featureCategory);
    }

    public Task<Result<CoreFeatureCategory>> UpdateAsync(CoreFeatureCategory featureCategory)
    {
        CoreFeatureCategory? existingCategory = _featureCategories.FirstOrDefault(fc => fc.Id == featureCategory.Id);
        if (existingCategory == null)
        {
            return Task.FromResult(Result<CoreFeatureCategory>.Failure("FeatureCategory not found for update."));
        }

        _featureCategories.Remove(existingCategory);
        _featureCategories.Add(featureCategory);
        return Task.FromResult(Result<CoreFeatureCategory>.Success(featureCategory));
    }

    public Task<IEnumerable<FeatureCategoryReadModel>> GetAllAsync()
    {
        var featureCategories = _featureCategories
            .Select(fc => new FeatureCategoryReadModel(fc.Id, fc.Name))
            .ToList();

        return Task.FromResult<IEnumerable<FeatureCategoryReadModel>>(featureCategories);
    }

    public Task<IEnumerable<FeatureCategoryReadModel>> GetFeatureCategoriesByNamesAsync(IEnumerable<string> names)
    {
        var filteredCategories = _featureCategories
            .Where(fc => names.Contains(fc.Name.Value))
            .Select(fc => new FeatureCategoryReadModel(fc.Id, fc.Name))
            .ToList();

        return Task.FromResult<IEnumerable<FeatureCategoryReadModel>>(filteredCategories);
    }

    public Task<ISet<CoreFeatureCategory>> GetByNamesAsync(ICollection<string> names)
    {
        var results = (ISet<CoreFeatureCategory>)_featureCategories
            .Where(fc => names.Contains(fc.Name.Value))
            .ToHashSet();

        return Task.FromResult(results);
    }
}

// Helper class for seeding data, assuming ITimeProvider is needed
public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
