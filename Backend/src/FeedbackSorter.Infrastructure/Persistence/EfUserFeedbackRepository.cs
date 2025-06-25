using FeedbackSorter.Application.Feedback;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Infrastructure.Persistence.Mappers;
using FeedbackSorter.Infrastructure.Persistence.Models;
using FeedbackSorter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Infrastructure.Persistence;

public class EfUserFeedbackRepository : IUserFeedbackRepository
{
    private readonly FeedbackSorterDbContext _dbContext;
    private readonly ILogger<EfUserFeedbackRepository> _logger;

    public EfUserFeedbackRepository(FeedbackSorterDbContext dbContext, ILogger<EfUserFeedbackRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<UserFeedback>> GetByIdAsync(FeedbackId id)
    {
        _logger.LogDebug("Entering {MethodName} with id: {Id}", nameof(GetByIdAsync), id);

        UserFeedbackDb? userFeedbackDb = await _dbContext.UserFeedbacks
            .Include(uf => uf.AnalysisResultFeatureCategories)
            .Include(uf => uf.SelectedFeedbackCategories)
            .FirstOrDefaultAsync(uf => uf.Id == id.Value);

        _logger.LogDebug("11");
        if (userFeedbackDb == null)
        {
            return Result<UserFeedback>.Failure($"UserFeedback with Id {id.Value} not found.");
        }

        return Result<UserFeedback>.Success(UserFeedbackMapper.ToDomainEntity(userFeedbackDb));
    }

    public async Task<Result<UserFeedback>> AddAsync(UserFeedback userFeedback)
    {
        UserFeedbackDb userFeedbackDb = UserFeedbackMapper.ToDbEntity(userFeedback);
        await _dbContext.UserFeedbacks.AddAsync(userFeedbackDb);
        await _dbContext.SaveChangesAsync();
        return Result<UserFeedback>.Success(userFeedback);
    }

    public async Task<Result<UserFeedback>> UpdateAsync(UserFeedback userFeedback)
    {
        UserFeedbackDb? existingUserFeedbackDb = await _dbContext.UserFeedbacks
            .Include(uf => uf.AnalysisResultFeatureCategories)
            .Include(uf => uf.SelectedFeedbackCategories)
            .FirstOrDefaultAsync(uf => uf.Id == userFeedback.Id.Value);

        if (existingUserFeedbackDb == null)
        {
            return Result<UserFeedback>.Failure($"UserFeedback with Id {userFeedback.Id.Value} not found for update.");
        }

        UserFeedbackDb newUserFeedbackDb = UserFeedbackMapper.ToDbEntity(userFeedback);
        existingUserFeedbackDb.OverwriteDataFrom(newUserFeedbackDb);

        // Reconcile FeatureCategories with DbContext tracking
        if (existingUserFeedbackDb.AnalysisResultFeatureCategories != null)
        {
            var reconciledFeatureCategories = new List<FeatureCategoryDb>();
            foreach (FeatureCategoryDb fc in existingUserFeedbackDb.AnalysisResultFeatureCategories)
            {
                FeatureCategoryDb? existingFc = await _dbContext.FeatureCategories.FirstOrDefaultAsync(f => f.Id == fc.Id);
                if (existingFc != null)
                {
                    reconciledFeatureCategories.Add(existingFc);
                }
                else
                {
                    _dbContext.FeatureCategories.Add(fc);
                    reconciledFeatureCategories.Add(fc);
                }
            }
            existingUserFeedbackDb.AnalysisResultFeatureCategories.Clear();
            foreach (FeatureCategoryDb fc in reconciledFeatureCategories)
            {
                existingUserFeedbackDb.AnalysisResultFeatureCategories.Add(fc);
            }
        }

        var currentDbCategoryValues = existingUserFeedbackDb.SelectedFeedbackCategories
                                        .Select(sfc => Enum.Parse<FeedbackCategoryType>(sfc.FeedbackCategoryValue))
                                        .ToHashSet();
        HashSet<FeedbackCategoryType> desiredDomainCategoryTypes = userFeedback.AnalysisResult?.FeedbackCategories?.ToHashSet() ?? new HashSet<FeedbackCategoryType>();

        // Remove categories no longer present in the domain entity
        var categoriesToRemove = existingUserFeedbackDb.SelectedFeedbackCategories
                                    .Where(sfc => !desiredDomainCategoryTypes.Contains(Enum.Parse<FeedbackCategoryType>(sfc.FeedbackCategoryValue)))
                                    .ToList();
        foreach (UserFeedbackSelectedCategoryDb? sfcDb in categoriesToRemove)
        {
            _dbContext.UserFeedbackSelectedCategories.Remove(sfcDb);
        }

        // Add new categories from the domain entity
        foreach (FeedbackCategoryType desiredType in desiredDomainCategoryTypes)
        {
            if (!currentDbCategoryValues.Contains(desiredType))
            {
                existingUserFeedbackDb.SelectedFeedbackCategories.Add(new UserFeedbackSelectedCategoryDb
                {
                    UserFeedbackDbId = existingUserFeedbackDb.Id,
                    FeedbackCategoryValue = desiredType.ToString()
                });
            }
        }

        await _dbContext.SaveChangesAsync();
        return Result<UserFeedback>.Success(userFeedback);
    }
}
