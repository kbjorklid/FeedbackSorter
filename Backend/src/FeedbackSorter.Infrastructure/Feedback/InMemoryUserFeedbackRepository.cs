using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.FeatureCategories.Queries;
using FeedbackSorter.Application.UserFeedback;
using FeedbackSorter.Application.UserFeedback.Queries;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Infrastructure.Feedback;

public class InMemoryUserFeedbackRepository : IUserFeedbackRepository, IUserFeedbackReadRepository
{
    private readonly Dictionary<FeedbackId, UserFeedback> _userFeedbacks = new();
    private readonly IFeatureCategoryReadRepository _featureCategoryReadRepository;

    public InMemoryUserFeedbackRepository(IFeatureCategoryReadRepository featureCategoryReadRepository)
    {
        _featureCategoryReadRepository = featureCategoryReadRepository;
    }

    public Task<Result<UserFeedback>> GetByIdAsync(FeedbackId id)
    {
        if (_userFeedbacks.TryGetValue(id, out UserFeedback? userFeedback))
        {
            return Task.FromResult(Result<UserFeedback>.Success(userFeedback));
        }
        return Task.FromResult(Result<UserFeedback>.Failure($"UserFeedback with ID {id.Value} not found."));
    }

    public Task<Result<UserFeedback>> AddAsync(UserFeedback userFeedback)
    {
        if (_userFeedbacks.ContainsKey(userFeedback.Id))
        {
            return Task.FromResult(Result<UserFeedback>.Failure($"UserFeedback with ID {userFeedback.Id.Value} already exists."));
        }
        _userFeedbacks.Add(userFeedback.Id, userFeedback);
        return Task.FromResult(Result<UserFeedback>.Success(userFeedback));
    }

    public Task<Result<UserFeedback>> UpdateAsync(UserFeedback userFeedback)
    {
        if (!_userFeedbacks.ContainsKey(userFeedback.Id))
        {
            return Task.FromResult(Result<UserFeedback>.Failure($"UserFeedback with ID {userFeedback.Id.Value} not found for update."));
        }
        _userFeedbacks[userFeedback.Id] = userFeedback;
        return Task.FromResult(Result<UserFeedback>.Success(userFeedback));
    }

    public async Task<PagedResult<AnalyzedFeedbackReadModel>> GetPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize)
    {
        IQueryable<UserFeedback> query = _userFeedbacks.Values.AsQueryable();

        if (filter.FeedbackCategories != null && filter.FeedbackCategories.Any())
        {
            query = query.Where(uf => uf.AnalysisResult != null && uf.AnalysisResult.FeedbackCategories.Any(fc => filter.FeedbackCategories.Contains(fc)));
        }

        if (filter.FeatureCategoryIds != null && filter.FeatureCategoryIds.Any())
        {
            query = query.Where(uf => uf.AnalysisResult != null && uf.AnalysisResult.FeatureCategoryIds.Any(fci => filter.FeatureCategoryIds.Contains(fci)));
        }

        query = query.Where(uf => uf.AnalysisStatus == AnalysisStatus.Analyzed);

        // Calculate total count before sorting and pagination
        int totalCount = query.Count();
        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        query = ApplySorting(query, filter.SortBy, filter.SortAscending);

        // Fetch all feature categories once for mapping
        IEnumerable<FeatureCategoryReadModel> allFeatureCategories = await _featureCategoryReadRepository.GetAllAsync();

        var pagedList = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(uf => MapToAnalyzedReadModel(uf, allFeatureCategories))
            .ToList();

        var pagedResult = new PagedResult<AnalyzedFeedbackReadModel>(pagedList, pageNumber, pageSize, totalCount);

        return pagedResult;
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

    private AnalyzedFeedbackReadModel MapToAnalyzedReadModel(UserFeedback userFeedback, IEnumerable<FeatureCategoryReadModel> allFeatureCategories)
    {
        // This method should only be called for successfully analyzed feedback
        if (userFeedback.AnalysisResult == null)
        {
            throw new InvalidOperationException("Cannot map unanalyzed feedback to AnalyzedFeedbackReadModel.");
        }

        return new AnalyzedFeedbackReadModel
        {
            Id = userFeedback.Id,
            Title = userFeedback.AnalysisResult.Title.Value,
            SubmittedAt = userFeedback.SubmittedAt.Value,
            FeedbackCategories = userFeedback.AnalysisResult.FeedbackCategories,
            FeatureCategories = allFeatureCategories
                .Where(fc => userFeedback.AnalysisResult.FeatureCategoryIds.Any(id => id.Value == fc.Id.Value)),
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
