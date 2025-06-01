using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Infrastructure.Persistence.Mappers;
using FeedbackSorter.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace FeedbackSorter.Infrastructure.Persistence;

public class EfFeatureCategoryReadRepository : IFeatureCategoryReadRepository
{
    private readonly FeedbackSorterDbContext _dbContext;

    public EfFeatureCategoryReadRepository(FeedbackSorterDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<FeatureCategoryReadModel>> GetAllAsync()
    {
        List<FeatureCategoryDb> featureCategoryDbs = await _dbContext.FeatureCategories
            .AsNoTracking()
            .ToListAsync();

        return featureCategoryDbs.Select(FeatureCategoryMapper.ToReadModel);
    }

    public async Task<IEnumerable<FeatureCategoryReadModel>> GetFeatureCategoriesByNamesAsync(IEnumerable<string> names)
    {
        List<FeatureCategoryDb> featureCategoryDbs = await _dbContext.FeatureCategories
            .AsNoTracking()
            .Where(fc => names.Contains(fc.Name))
            .ToListAsync();

        return featureCategoryDbs.Select(FeatureCategoryMapper.ToReadModel);
    }
}
