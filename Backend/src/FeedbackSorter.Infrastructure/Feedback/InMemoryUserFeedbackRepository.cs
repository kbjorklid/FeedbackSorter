using FeedbackSorter.Application.FeatureCategories.Queries;
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
        Console.WriteLine($"Added user feedback with ID: {userFeedback.Id.Value}");
        return Task.FromResult(Result<UserFeedback>.Success(userFeedback));
    }

    public Task<Result<UserFeedback>> UpdateAsync(UserFeedback userFeedback)
    {
        if (!_userFeedbacks.ContainsKey(userFeedback.Id))
        {
            Console.WriteLine("no result, cannot update");
            return Task.FromResult(Result<UserFeedback>.Failure($"UserFeedback with ID {userFeedback.Id.Value} not found for update."));
        }
        _userFeedbacks[userFeedback.Id] = userFeedback;

        Console.WriteLine("updated, " + userFeedback.AnalysisStatus + " " + userFeedback.Id.Value);
        return Task.FromResult(Result<UserFeedback>.Success(userFeedback));
    }

    public Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> GetPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize)
    {

        Console.WriteLine($"GetPagedListAsync, contains : {_userFeedbacks.Count}");
        IQueryable<UserFeedback> query = _userFeedbacks.Values.AsQueryable();

        Console.WriteLine("start" + query.Count());

        if (filter.FeedbackCategories != null && filter.FeedbackCategories.Any())
        {

            query = query.Where(uf => uf.AnalysisResult != null && uf.AnalysisResult.FeedbackCategories.Any(fc => filter.FeedbackCategories.Contains(fc)));

            Console.WriteLine("fbcat" + query.Count());
        }

        if (filter.FeatureCategoryIds != null && filter.FeatureCategoryIds.Any())
        {
            Console.WriteLine("feature cats" + string.Join(", ", filter.FeatureCategoryIds));
            query = query.Where(uf => uf.AnalysisResult != null
                && uf.AnalysisResult.FeatureCategories.Any(fc => filter.FeatureCategoryIds.Contains(fc.Id)));

            Console.WriteLine("feacat" + query.Count());
        }

        query = query.Where(uf => uf.AnalysisStatus == AnalysisStatus.Analyzed);

        Console.WriteLine("analyzed" + query.Count());

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
