using FeedbackSorter.Application.UserFeedback;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Infrastructure.Persistence.Models;
using FeedbackSorter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using CoreUserFeedback = FeedbackSorter.Core.Feedback.UserFeedback;

namespace FeedbackSorter.Infrastructure.Persistence;

public class EfUserFeedbackRepository : IUserFeedbackRepository
{
    private readonly FeedbackSorterDbContext _dbContext;

    public EfUserFeedbackRepository(FeedbackSorterDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CoreUserFeedback>> GetByIdAsync(FeedbackId id)
    {
        UserFeedbackDb? userFeedbackDb = await _dbContext.UserFeedbacks
            .Include(uf => uf.AnalysisResultFeatureCategories)
            .Include(uf => uf.SelectedFeedbackCategories)
            .FirstOrDefaultAsync(uf => uf.Id == id.Value);

        if (userFeedbackDb == null)
        {
            return Result<CoreUserFeedback>.Failure($"UserFeedback with Id {id.Value} not found.");
        }

        return Result<CoreUserFeedback>.Success(UserFeedbackMapper.ToDomainEntity(userFeedbackDb));
    }

    public async Task<Result<CoreUserFeedback>> AddAsync(CoreUserFeedback userFeedback)
    {
        UserFeedbackDb userFeedbackDb = UserFeedbackMapper.ToDbEntity(userFeedback);
        await _dbContext.UserFeedbacks.AddAsync(userFeedbackDb);
        await _dbContext.SaveChangesAsync();
        return Result<CoreUserFeedback>.Success(userFeedback);
    }

    public async Task<Result<CoreUserFeedback>> UpdateAsync(CoreUserFeedback userFeedback)
    {
        UserFeedbackDb? existingUserFeedbackDb = await _dbContext.UserFeedbacks
            .Include(uf => uf.AnalysisResultFeatureCategories)
            .Include(uf => uf.SelectedFeedbackCategories)
            .FirstOrDefaultAsync(uf => uf.Id == userFeedback.Id.Value);

        if (existingUserFeedbackDb == null)
        {
            return Result<CoreUserFeedback>.Failure($"UserFeedback with Id {userFeedback.Id.Value} not found for update.");
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
        return Result<CoreUserFeedback>.Success(userFeedback);
    }
}
