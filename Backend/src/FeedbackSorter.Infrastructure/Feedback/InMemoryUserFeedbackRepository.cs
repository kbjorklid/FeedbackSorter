using FeedbackSorter.Application.UserFeedback;
using FeedbackSorter.Application.UserFeedback.Queries;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
namespace FeedbackSorter.Infrastructure.Feedback;

public class InMemoryUserFeedbackRepository : IUserFeedbackRepository, IUserFeedbackReadRepository
{
    private readonly Dictionary<FeedbackId, UserFeedback> _userFeedbacks = new();

    public Task<Result<UserFeedback>> GetByIdAsync(FeedbackId id)
    {
        if (_userFeedbacks.TryGetValue(id, out var userFeedback))
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

    public Task<IEnumerable<AnalyzedFeedbackReadModel>> GetPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize)
    {
        var query = _userFeedbacks.Values.AsQueryable();

        if (filter.FeedbackCategories != null && filter.FeedbackCategories.Any())
        {
            query = query.Where(uf => uf.AnalysisResult != null && uf.AnalysisResult.FeedbackCategories.Any(fc => filter.FeedbackCategories.Contains(fc)));
        }

        if (filter.FeatureCategoryIds != null && filter.FeatureCategoryIds.Any())
        {
            query = query.Where(uf => uf.AnalysisResult != null && uf.AnalysisResult.FeatureCategoryIds.Any(fci => filter.FeatureCategoryIds.Contains(fci)));
        }

        // Only return successfully analyzed feedback for this method
        query = query.Where(uf => uf.AnalysisStatus == AnalysisStatus.Analyzed);

        query = ApplySorting(query, filter.SortBy, filter.SortAscending);

        var pagedList = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var readModels = pagedList.Select(MapToAnalyzedReadModel);

        return Task.FromResult(readModels.AsEnumerable());
    }

    public Task<IEnumerable<FailedToAnalyzeFeedbackReadModel>> GetFailedAnalysisPagedListAsync(FailedToAnalyzeUserFeedbackFilter filter, int pageNumber, int pageSize)
    {
        var query = _userFeedbacks.Values.AsQueryable()
            .Where(uf => uf.AnalysisStatus == AnalysisStatus.AnalysisFailed);

        query = ApplySorting(query, filter.SortBy, filter.SortAscending);

        var pagedList = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var readModels = pagedList.Select(MapToFailedToAnalyzeReadModel);

        return Task.FromResult(readModels.AsEnumerable());
    }

    private static IQueryable<UserFeedback> ApplySorting(IQueryable<UserFeedback> query, string? sortBy, bool sortAscending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "title" => sortAscending ? query.OrderBy(uf => uf.AnalysisResult != null ? uf.AnalysisResult.Title.Value : uf.Text.Value.Substring(0, Math.Min(uf.Text.Value.Length, 30))) : query.OrderByDescending(uf => uf.AnalysisResult != null ? uf.AnalysisResult.Title.Value : uf.Text.Value.Substring(0, Math.Min(uf.Text.Value.Length, 30))),
            "submittedat" => sortAscending ? query.OrderBy(uf => uf.SubmittedAt.Value) : query.OrderByDescending(uf => uf.SubmittedAt.Value),
            _ => query.OrderBy(uf => uf.SubmittedAt.Value) // Default sort
        };
    }

    private static AnalyzedFeedbackReadModel MapToAnalyzedReadModel(UserFeedback userFeedback)
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
            FeatureCategoryIds = userFeedback.AnalysisResult.FeatureCategoryIds,
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
