
using FeedbackSorter.Application.Feedback.Repositories;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Core;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Infrastructure.Persistence.Mappers;
using FeedbackSorter.Infrastructure.Persistence.Models;
using FeedbackSorter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Infrastructure.Persistence;

public class EfUserFeedbackRepository(FeedbackSorterDbContext dbContext, ILogger<EfUserFeedbackRepository> logger)
    : IUserFeedbackRepository
{
    public async Task<Result<UserFeedback>> GetByIdAsync(FeedbackId id)
    {

        UserFeedbackDb? userFeedbackDb = await dbContext.UserFeedbacks
            .Include(uf => uf.AnalysisResultFeatureCategories)
            .Include(uf => uf.SelectedFeedbackCategories)
            .FirstOrDefaultAsync(uf => uf.Id == id.Value);

        if (userFeedbackDb == null)
        {
            logger.LogWarning("Feedback not found {FeedbackId}", id.Value);
            return Result<UserFeedback>.Failure($"UserFeedback with Id {id.Value} not found.");
        }

        return Result<UserFeedback>.Success(UserFeedbackMapper.ToDomainEntity(userFeedbackDb));
    }

    public async Task<UserFeedback> AddAsync(UserFeedback userFeedback)
    {
        UserFeedbackDb userFeedbackDb = UserFeedbackMapper.ToDbEntity(userFeedback);
        await dbContext.UserFeedbacks.AddAsync(userFeedbackDb);
        await dbContext.SaveChangesAsync();
        return userFeedback;
    }

    public async Task<Result<UserFeedback>> UpdateAsync(UserFeedback userFeedback)
    {
        UserFeedbackDb? existingUserFeedbackDb = await dbContext.UserFeedbacks
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
                FeatureCategoryDb? existingFc = await dbContext.FeatureCategories.FirstOrDefaultAsync(f => f.Id == fc.Id);
                if (existingFc != null)
                {
                    reconciledFeatureCategories.Add(existingFc);
                }
                else
                {
                    dbContext.FeatureCategories.Add(fc);
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
            dbContext.UserFeedbackSelectedCategories.Remove(sfcDb);
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

        await dbContext.SaveChangesAsync();
        return Result<UserFeedback>.Success(userFeedback);
    }

    public async Task<IList<UserFeedback>> QueryAsync(UserFeedbackQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<UserFeedbackDb> queryable = dbContext.UserFeedbacks
            .Include(uf => uf.AnalysisResultFeatureCategories)
            .Include(uf => uf.SelectedFeedbackCategories);

        if (query.AnalysisStatuses != null && query.AnalysisStatuses.Any())
        {
            ISet<string> analysisStatuses = query.AnalysisStatuses.Select(s => s.ToString()).ToHashSet();
            queryable = queryable.Where(uf => analysisStatuses.Contains(uf.AnalysisStatus));
        }

        queryable = query.SortBy switch
        {
            UserFeedbackSortBy.SubmittedAt => query.Order == SortOrder.Asc
                ? queryable.OrderBy(uf => uf.SubmittedAt)
                : queryable.OrderByDescending(uf => uf.SubmittedAt),
            UserFeedbackSortBy.Title => query.Order == SortOrder.Asc
                ? queryable.OrderBy(uf => uf.AnalysisResultTitle)
                : queryable.OrderByDescending(uf => uf.AnalysisResultTitle),
            _ => queryable.OrderBy(uf => uf.SubmittedAt)
        };

        if (query.MaxResults.HasValue)
        {
            queryable = queryable.Take(query.MaxResults.Value);
        }

        List<UserFeedbackDb> userFeedbackDbs = await queryable.ToListAsync(cancellationToken: cancellationToken);
        IList<UserFeedback> userFeedbacks = userFeedbackDbs.Select(dbEntity => UserFeedbackMapper.ToDomainEntity(dbEntity)).ToList();

        return userFeedbacks;
    }

    public async Task<bool> DeleteAsync(FeedbackId id)
    {
        UserFeedbackDb? userFeedbackDb = await dbContext.UserFeedbacks
            .FirstOrDefaultAsync(uf => uf.Id == id.Value);

        if (userFeedbackDb == null)
        {
            logger.LogWarning("Feedback not found for deletion {FeedbackId}", id.Value);
            return false;
        }

        dbContext.UserFeedbacks.Remove(userFeedbackDb);
        await dbContext.SaveChangesAsync();
        return true;
    }
}
