using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Repositories;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackReadRepository;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Infrastructure.Persistence.Mappers;
using FeedbackSorter.Infrastructure.Persistence.Models;
using FeedbackSorter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Infrastructure.Persistence;

public class EfUserFeedbackReadRepository : IUserFeedbackReadRepository
{
    private readonly FeedbackSorterDbContext _dbContext;
    private readonly ILogger<EfUserFeedbackReadRepository> _logger;

    public EfUserFeedbackReadRepository(FeedbackSorterDbContext dbContext, ILogger<EfUserFeedbackReadRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> GetPagedListAsync(AnalyzedFeedbackQueryParams filter, int pageNumber, int pageSize)
    {
        _logger.LogDebug("Getting paged list of analyzed feedbacks with filter: {Filter}, pageNumber: {PageNumber}, pageSize: {PageSize}",
                filter, pageNumber, pageSize);

        IQueryable<UserFeedbackDb> query = _dbContext.UserFeedbacks
            .AsNoTracking()
            .Where(uf => uf.AnalysisStatus == AnalysisStatus.Analyzed.ToString())
            .Include(uf => uf.AnalysisResultFeatureCategories)
            .Include(uf => uf.SelectedFeedbackCategories)
            .AsQueryable();

        // Apply filters
        if (filter.Sentiment != null)
        {
            query = query.Where(uf => uf.AnalysisResultSentiment != null && uf.AnalysisResultSentiment == filter.Sentiment.ToString());
        }
        
        if (filter.FeedbackCategories != null && filter.FeedbackCategories.Any())
        {
            List<string> catStrings = filter.FeedbackCategories.Select(f => f.ToString()).ToList();
            query = query.Where(uf => uf.SelectedFeedbackCategories.Any(sfc => catStrings.Contains(sfc.FeedbackCategoryValue)));
        }

        if (filter.FeatureCategoryNames != null && filter.FeatureCategoryNames.Any())
        {
            var feaCategoryNames = filter.FeatureCategoryNames.Select(f => f.Value.ToLowerInvariant()).ToList();
            query = query.Where(uf => uf.AnalysisResultFeatureCategories.Any(fc => feaCategoryNames.Contains(fc.Name.ToLower())));
        }

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
        _logger.LogDebug("Total count of analyzed feedbacks: {TotalCount}", totalCount);

        List<UserFeedbackDb> items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        _logger.LogDebug("Retrieved {ItemCount} items for page {PageNumber} with page size {PageSize}", items.Count, pageNumber, pageSize);

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

        return new PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>(readModels, pageNumber, pageSize, totalCount);
    }

    public async Task<PagedResult<FailedToAnalyzeFeedbackReadModel>> GetFailedAnalysisPagedListAsync(int pageNumber, int pageSize)
    {
        IQueryable<UserFeedbackDb> query = _dbContext.UserFeedbacks
            .AsNoTracking()
            .Where(uf => uf.AnalysisStatus == AnalysisStatus.AnalysisFailed.ToString())
            .AsQueryable();

        int totalCount = await query.CountAsync();

        List<UserFeedbackDb> items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        _logger.LogDebug("Retrieved {ItemCount} items for page {PageNumber} with page size {PageSize}", items.Count, pageNumber, pageSize);

        List<FailedToAnalyzeFeedbackReadModel> readModels = items.Select(uf => new FailedToAnalyzeFeedbackReadModel
        {
            Id = FeedbackId.FromGuid(uf.Id),
            SubmittedAt = uf.SubmittedAt,
            RetryCount = uf.RetryCount,
            FullFeedbackText = uf.Text
        }).ToList();

        _logger.LogInformation("Successfully retrieved paged list of failed analysis feedbacks.");

        return new PagedResult<FailedToAnalyzeFeedbackReadModel>(readModels, pageNumber, pageSize, totalCount);
    }
}
