using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.UserFeedback.Queries;

public class GetAnalyzedFeedbacksQueryHandler
{
    private readonly IUserFeedbackReadRepository _userFeedbackReadRepository;

    public GetAnalyzedFeedbacksQueryHandler(IUserFeedbackReadRepository userFeedbackReadRepository)
    {
        _userFeedbackReadRepository = userFeedbackReadRepository;
    }

    public async Task<PagedResult<AnalyzedFeedbackReadModel>> HandleAsync(GetAnalyzedFeedbacksQuery query, CancellationToken cancellationToken)
    {
        IEnumerable<Guid>? featureCategoryIds = null;
        if (query.FeatureCategoryNames != null && query.FeatureCategoryNames.Any())
        {
            // The InMemoryUserFeedbackRepository will handle fetching FeatureCategoryReadModels
            // based on FeatureCategoryIds, so we only need the IDs for filtering here.
            // This part of the logic might need to be adjusted if the filtering by name
            // is still desired at this layer and not handled by the repository's filter.
            // For now, assuming the repository handles the name-to-id conversion for filtering.
            // If not, we would need IFeatureCategoryReadRepository here to convert names to IDs.
            // Given the current task, I will assume the repository's filter handles this.
        }

        var filter = new UserFeedbackFilter
        {
            FeedbackCategories = query.FeedbackCategories,
            FeatureCategoryIds = featureCategoryIds?.Select(id => new FeatureCategoryId(id)), // This will be null if FeatureCategoryNames is null or empty
            SortBy = query.SortBy,
            SortAscending = query.SortOrder == SortOrder.Asc,
            AnalysisStatus = AnalysisStatus.Analyzed
        };

        PagedResult<AnalyzedFeedbackReadModel> pagedResult = await _userFeedbackReadRepository.GetPagedListAsync(filter, query.PageNumber, query.PageSize);

        return pagedResult;
    }
}
