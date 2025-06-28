using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Infrastructure.Persistence.Mappers;
using FeedbackSorter.Infrastructure.Persistence.Models;
using FeedbackSorter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using CoreFeatureCategory = FeedbackSorter.Core.FeatureCategories.FeatureCategory;

namespace FeedbackSorter.Infrastructure.Persistence;

public class EfFeatureCategoryRepository : IFeatureCategoryRepository
{
    private readonly FeedbackSorterDbContext _dbContext;

    public EfFeatureCategoryRepository(FeedbackSorterDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CoreFeatureCategory>> GetByIdAsync(FeatureCategoryId id)
    {
        FeatureCategoryDb? featureCategoryDb = await _dbContext.FeatureCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(fc => fc.Id == id.Value);

        if (featureCategoryDb == null)
        {
            return Result<CoreFeatureCategory>.Failure($"FeatureCategory with Id {id.Value} not found.");
        }

        return Result<CoreFeatureCategory>.Success(FeatureCategoryMapper.ToDomainEntity(featureCategoryDb));
    }

    public async Task<ISet<CoreFeatureCategory>> GetByNamesAsync(ICollection<string> names)
    {
        List<FeatureCategoryDb> featureCategoryDbs = await _dbContext.FeatureCategories
            .AsNoTracking()
            .Where(fc => names.Contains(fc.Name))
            .ToListAsync();

        return featureCategoryDbs.Select(FeatureCategoryMapper.ToDomainEntity).ToHashSet();
    }

    public async Task<CoreFeatureCategory> AddAsync(CoreFeatureCategory featureCategory)
    {
        FeatureCategoryDb featureCategoryDb = FeatureCategoryMapper.ToDbEntity(featureCategory);
        await _dbContext.FeatureCategories.AddAsync(featureCategoryDb);
        await _dbContext.SaveChangesAsync();
        return featureCategory;
    }

    public async Task<Result<CoreFeatureCategory>> UpdateAsync(CoreFeatureCategory featureCategory)
    {
        FeatureCategoryDb? existingFeatureCategoryDb = await _dbContext.FeatureCategories
            .FirstOrDefaultAsync(fc => fc.Id == featureCategory.Id.Value);

        if (existingFeatureCategoryDb == null)
        {
            return Result<CoreFeatureCategory>.Failure($"FeatureCategory with Id {featureCategory.Id.Value} not found for update.");
        }

        existingFeatureCategoryDb.Name = featureCategory.Name.Value;
        // CreatedAt is not updated as it's a creation timestamp

        await _dbContext.SaveChangesAsync();
        return Result<CoreFeatureCategory>.Success(featureCategory);
    }
}
