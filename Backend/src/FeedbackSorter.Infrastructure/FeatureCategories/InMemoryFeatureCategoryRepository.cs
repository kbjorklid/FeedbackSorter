using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.SharedKernel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CoreFeatureCategory = FeedbackSorter.Core.FeatureCategories.FeatureCategory;

namespace FeedbackSorter.Infrastructure.FeatureCategories;

public class InMemoryFeatureCategoryRepository : IFeatureCategoryRepository
{
    private readonly List<CoreFeatureCategory> _featureCategories;

    public InMemoryFeatureCategoryRepository(List<CoreFeatureCategory> featureCategories)
    {
        _featureCategories = featureCategories;
    }

    public Task<Result<CoreFeatureCategory>> GetByIdAsync(FeatureCategoryId id)
    {
        var featureCategory = _featureCategories.FirstOrDefault(fc => fc.Id == id);
        return Task.FromResult(featureCategory != null
            ? Result<CoreFeatureCategory>.Success(featureCategory)
            : Result<CoreFeatureCategory>.Failure("FeatureCategory not found."));
    }

    public Task<Result<CoreFeatureCategory>> AddAsync(CoreFeatureCategory featureCategory)
    {
        if (_featureCategories.Any(fc => fc.Id == featureCategory.Id))
        {
            return Task.FromResult(Result<CoreFeatureCategory>.Failure("FeatureCategory with this ID already exists."));
        }
        _featureCategories.Add(featureCategory);
        return Task.FromResult(Result<CoreFeatureCategory>.Success(featureCategory));
    }

    public Task<Result<CoreFeatureCategory>> UpdateAsync(CoreFeatureCategory featureCategory)
    {
        var existingCategory = _featureCategories.FirstOrDefault(fc => fc.Id == featureCategory.Id);
        if (existingCategory == null)
        {
            return Task.FromResult(Result<CoreFeatureCategory>.Failure("FeatureCategory not found for update."));
        }

        // In a real scenario, you might update properties individually.
        // For in-memory, replacing the object or updating its mutable properties is fine.
        _featureCategories.Remove(existingCategory);
        _featureCategories.Add(featureCategory);
        return Task.FromResult(Result<CoreFeatureCategory>.Success(featureCategory));
    }
}

// Helper class for seeding data, assuming ITimeProvider is needed
public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
