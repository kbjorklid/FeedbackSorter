using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.Feedback;
using FeedbackSorter.Application.Feedback.GetAnalyzedFeedbacks;
using FeedbackSorter.Application.Feedback.Queries;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Infrastructure.Feedback;

public class InMemoryUserFeedbackRepository : IUserFeedbackRepository, IUserFeedbackReadRepository
{
    private readonly Dictionary<FeedbackId, UserFeedback> _userFeedbacks = new();
    private readonly ILogger<InMemoryUserFeedbackRepository> _logger;

    public InMemoryUserFeedbackRepository(ILogger<InMemoryUserFeedbackRepository> logger)
    {
        _logger = logger;
    }

    public Task<Result<UserFeedback>> GetByIdAsync(FeedbackId id)
    {
        if (_userFeedbacks.TryGetValue(id, out UserFeedback? userFeedback))
        {
            return Task.FromResult(Result<UserFeedback>.Success(userFeedback));
        }
        _logger.LogWarning("UserFeedback with ID {FeedbackId} not found.", id.Value);
        return Task.FromResult(Result<UserFeedback>.Failure($"UserFeedback with ID {id.Value} not found."));
    }

    public Task<Result<UserFeedback>> AddAsync(UserFeedback userFeedback)
    {
        if (_userFeedbacks.ContainsKey(userFeedback.Id))
        {
            _logger.LogWarning("UserFeedback with ID {FeedbackId} already exists.", userFeedback.Id.Value);
            return Task.FromResult(Result<UserFeedback>.Failure($"UserFeedback with ID {userFeedback.Id.Value} already exists."));
        }
        _userFeedbacks.Add(userFeedback.Id, userFeedback);
        _logger.LogInformation("Added user feedback with ID: {FeedbackId}", userFeedback.Id.Value);
        return Task.FromResult(Result<UserFeedback>.Success(userFeedback));
    }

    public Task<Result<UserFeedback>> UpdateAsync(UserFeedback userFeedback)
    {
        if (!_userFeedbacks.ContainsKey(userFeedback.Id))
        {
            _logger.LogWarning("No result, cannot update. UserFeedback with ID {FeedbackId} not found for update.", userFeedback.Id.Value);
            return Task.FromResult(Result<UserFeedback>.Failure($"UserFeedback with ID {userFeedback.Id.Value} not found for update."));
        }
        _userFeedbacks[userFeedback.Id] = userFeedback;

        _logger.LogInformation("Updated user feedback with ID: {FeedbackId}, AnalysisStatus: {AnalysisStatus}", userFeedback.Id.Value, userFeedback.AnalysisStatus);
        return Task.FromResult(Result<UserFeedback>.Success(userFeedback));
    }

    public Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> GetPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize)
    {

        _logger.LogDebug("GetPagedListAsync, contains: {Count}", _userFeedbacks.Count);
        IQueryable<UserFeedback> query = _userFeedbacks.Values.AsQueryable();

        _logger.LogDebug("Start query count: {Count}", query.Count());

        if (filter.FeedbackCategories != null && filter.FeedbackCategories.Any())
        {

            query = query.Where(uf => uf.AnalysisResult != null && uf.AnalysisResult.FeedbackCategories.Any(fc => filter.FeedbackCategories.Contains(fc)));

            _logger.LogDebug("Feedback categories filter applied, count: {Count}", query.Count());
        }

        if (filter.FeatureCategoryIds != null && filter.FeatureCategoryIds.Any())
        {
            _logger.LogDebug("Feature categories filter applied: {FeatureCategoryIds}", string.Join(", ", filter.FeatureCategoryIds));
            query = query.Where(uf => uf.AnalysisResult != null
                && uf.AnalysisResult.FeatureCategories.Any(fc => filter.FeatureCategoryIds.Contains(fc.Id)));

            _logger.LogDebug("Feature categories filter applied, count: {Count}", query.Count());
        }

        query = query.Where(uf => uf.AnalysisStatus == AnalysisStatus.Analyzed);

        _logger.LogDebug("Analyzed status filter applied, count: {Count}", query.Count());

        // Calculate total count before sorting and pagination
        int totalCount = query.Count();
        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        query = ApplySorting(query, filter.SortBy, filter.SortAscending);

        var pagedList = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(uf => MapToAnalyzedReadModel(uf))
            .ToList();

        var pagedResult = new PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>(
            pagedList, pageNumber, pageSize, totalCount);

        return Task.FromResult(pagedResult);
    }

    public Task<List<FailedToAnalyzeFeedbackReadModel>> GetFailedAnalysisPagedListAsync(FailedToAnalyzeUserFeedbackFilter filter, int pageNumber, int pageSize)
    {
        IQueryable<UserFeedback> query = _userFeedbacks.Values.AsQueryable()
            .Where(uf => uf.AnalysisStatus == AnalysisStatus.AnalysisFailed);

        query = ApplySorting(query, filter.SortBy, filter.SortAscending);

        var pagedList = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToFailedToAnalyzeReadModel)
            .ToList();

        return Task.FromResult(pagedList);
    }

    private static IQueryable<UserFeedback> ApplySorting(IQueryable<UserFeedback> query, UserFeedbackSortBy? sortBy, bool sortAscending)
    {
        return sortBy switch
        {
            UserFeedbackSortBy.Title => sortAscending ? query.OrderBy(uf => uf.AnalysisResult != null ? uf.AnalysisResult.Title.Value : uf.Text.Value.Substring(0, Math.Min(uf.Text.Value.Length, 30))) : query.OrderByDescending(uf => uf.AnalysisResult != null ? uf.AnalysisResult.Title.Value : uf.Text.Value.Substring(0, Math.Min(uf.Text.Value.Length, 30))),
            UserFeedbackSortBy.SubmittedAt => sortAscending ? query.OrderBy(uf => uf.SubmittedAt.Value) : query.OrderByDescending(uf => uf.SubmittedAt.Value),
            _ => query.OrderBy(uf => uf.SubmittedAt.Value) // Default sort
        };
    }

    private AnalyzedFeedbackReadModel<FeatureCategoryReadModel> MapToAnalyzedReadModel(UserFeedback userFeedback)
    {
        // This method should only be called for successfully analyzed feedback
        if (userFeedback.AnalysisResult == null)
        {
            throw new InvalidOperationException("Cannot map unanalyzed feedback to AnalyzedFeedbackReadModel.");
        }

        return new AnalyzedFeedbackReadModel<FeatureCategoryReadModel>
        {
            Id = userFeedback.Id,
            Title = userFeedback.AnalysisResult.Title.Value,
            SubmittedAt = userFeedback.SubmittedAt.Value,
            FeedbackCategories = userFeedback.AnalysisResult.FeedbackCategories,
            FeatureCategories = userFeedback.AnalysisResult.FeatureCategories
                .Select(fc => new FeatureCategoryReadModel(fc))
                .ToHashSet(),
            Sentiment = userFeedback.AnalysisResult.Sentiment,
            FullFeedbackText = userFeedback.Text.Value
        };
    }

    private static FailedToAnalyzeFeedbackReadModel MapToFailedToAnalyzeReadModel(UserFeedback userFeedback)
    {
        return new FailedToAnalyzeFeedbackReadModel
        {
            Id = userFeedback.Id,
            TitleOrTruncatedText = userFeedback.AnalysisResult?.Title.Value ?? userFeedback.Text.Value.Substring(0, Math.Min(userFeedback.Text.Value.Length, 30)),
            SubmittedAt = userFeedback.SubmittedAt.Value,
            RetryCount = userFeedback.RetryCount,
            FullFeedbackText = userFeedback.Text.Value
        };
    }
}
