using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.UserFeedback;
using FeedbackSorter.Application.UserFeedback.GetAnalyzedFeedbacks;
using FeedbackSorter.Application.UserFeedback.Queries;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Infrastructure.Persistence.Mappers;
using FeedbackSorter.Infrastructure.Persistence.Models;
using FeedbackSorter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using CoreUserFeedback = FeedbackSorter.Core.Feedback.UserFeedback;

namespace FeedbackSorter.Infrastructure.Persistence;

public class EfUserFeedbackRepository : IUserFeedbackRepository, IUserFeedbackReadRepository
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


    public async Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> GetPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize)
    {
        IQueryable<UserFeedbackDb> query = _dbContext.UserFeedbacks
            .AsNoTracking()
            .Where(uf => uf.AnalysisStatus == AnalysisStatus.Analyzed.ToString())
            .Include(uf => uf.AnalysisResultFeatureCategories)
            .Include(uf => uf.SelectedFeedbackCategories)
            .AsQueryable();

        // Apply filters
        if (filter.FeedbackCategories != null && filter.FeedbackCategories.Any())
        {
            query = query.Where(uf => uf.SelectedFeedbackCategories.Any(sfc => filter.FeedbackCategories.Contains(Enum.Parse<FeedbackCategoryType>(sfc.FeedbackCategoryValue))));
        }

        if (filter.FeatureCategoryIds != null && filter.FeatureCategoryIds.Any())
        {
            query = query.Where(uf => uf.AnalysisResultFeatureCategories.Any(fc => filter.FeatureCategoryIds.Select(fci => fci.Value).Contains(fc.Id)));
        }

        // Apply sorting
        query = filter.SortBy switch
        {
            UserFeedbackSortBy.Title => filter.SortAscending
                ? query.OrderBy(uf => uf.AnalysisResultTitle)
                : query.OrderByDescending(uf => uf.AnalysisResultTitle),
            UserFeedbackSortBy.SubmittedAt => filter.SortAscending
                ? query.OrderBy(uf => uf.SubmittedAt)
                : query.OrderByDescending(uf => uf.SubmittedAt),
            _ => query.OrderByDescending(uf => uf.SubmittedAt) // Default sort
        };

        int totalCount = await query.CountAsync();
        List<UserFeedbackDb> items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var readModels = items.Select(uf => new AnalyzedFeedbackReadModel<FeatureCategoryReadModel>
        {
            Id = FeedbackId.FromGuid(uf.Id),
            Title = uf.AnalysisResultTitle!,
            SubmittedAt = uf.SubmittedAt,
            Sentiment = Enum.Parse<Sentiment>(uf.AnalysisResultSentiment!),
            FeedbackCategories = uf.SelectedFeedbackCategories.Select(sfc => Enum.Parse<FeedbackCategoryType>(sfc.FeedbackCategoryValue)).ToHashSet(),
            FeatureCategories = uf.AnalysisResultFeatureCategories.Select(FeatureCategoryMapper.ToReadModel).ToHashSet(),
            FullFeedbackText = uf.Text
        }).ToList();

        return new PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>(readModels, totalCount, pageNumber, pageSize);
    }

    public async Task<List<FailedToAnalyzeFeedbackReadModel>> GetFailedAnalysisPagedListAsync(FailedToAnalyzeUserFeedbackFilter filter, int pageNumber, int pageSize)
    {
        IQueryable<UserFeedbackDb> query = _dbContext.UserFeedbacks
            .AsNoTracking()
            .Where(uf => uf.AnalysisStatus == AnalysisStatus.AnalysisFailed.ToString())
            .AsQueryable();

        // Apply sorting
        query = filter.SortBy switch
        {
            UserFeedbackSortBy.Title => filter.SortAscending
                ? query.OrderBy(uf => uf.AnalysisResultTitle)
                : query.OrderByDescending(uf => uf.AnalysisResultTitle),
            UserFeedbackSortBy.SubmittedAt => filter.SortAscending
                ? query.OrderBy(uf => uf.SubmittedAt)
                : query.OrderByDescending(uf => uf.SubmittedAt),
            _ => query.OrderByDescending(uf => uf.SubmittedAt) // Default sort
        };

        List<UserFeedbackDb> items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var readModels = items.Select(uf => new FailedToAnalyzeFeedbackReadModel
        {
            Id = FeedbackId.FromGuid(uf.Id),
            TitleOrTruncatedText = uf.AnalysisResultTitle ?? uf.Text.Substring(0, Math.Min(uf.Text.Length, 30)), // Truncate if no title
            SubmittedAt = uf.SubmittedAt,
            RetryCount = uf.RetryCount,
            FullFeedbackText = uf.Text
        }).ToList();

        return readModels;
    }
}
