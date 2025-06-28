using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Queries.GetAnalyzedFeedbacks;
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

    public async Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> GetPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize)
    {
        _logger.LogDebug("Getting paged list of analyzed feedbacks with filter: {Filter}, pageNumber: {PageNumber}, pageSize: {PageSize}", filter, pageNumber, pageSize);

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

    public async Task<List<FailedToAnalyzeFeedbackReadModel>> GetFailedAnalysisPagedListAsync(FailedToAnalyzeUserFeedbackFilter filter, int pageNumber, int pageSize)
    {
        _logger.LogInformation("Getting paged list of failed analysis feedbacks with filter: {Filter}, pageNumber: {PageNumber}, pageSize: {PageSize}", filter, pageNumber, pageSize);

        IQueryable<UserFeedbackDb> query = _dbContext.UserFeedbacks
            .AsNoTracking()
            .Where(uf => uf.AnalysisStatus == AnalysisStatus.AnalysisFailed.ToString())
            .AsQueryable();

        // Apply sorting
        _logger.LogDebug("Applying sort by: {SortBy}, ascending: {SortAscending}", filter.SortBy, filter.SortAscending);
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
        _logger.LogDebug("Retrieved {ItemCount} items for page {PageNumber} with page size {PageSize}", items.Count, pageNumber, pageSize);

        var readModels = items.Select(uf => new FailedToAnalyzeFeedbackReadModel
        {
            Id = FeedbackId.FromGuid(uf.Id),
            TitleOrTruncatedText = uf.AnalysisResultTitle ?? uf.Text.Substring(0, Math.Min(uf.Text.Length, 30)), // Truncate if no title
            SubmittedAt = uf.SubmittedAt,
            RetryCount = uf.RetryCount,
            FullFeedbackText = uf.Text
        }).ToList();

        _logger.LogInformation("Successfully retrieved paged list of failed analysis feedbacks.");
        return readModels;
    }
}
