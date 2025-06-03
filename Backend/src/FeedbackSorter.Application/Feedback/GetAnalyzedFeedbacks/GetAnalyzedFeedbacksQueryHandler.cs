using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Core;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.GetAnalyzedFeedbacks;

public class GetAnalyzedFeedbacksQueryHandler
{
    private readonly IUserFeedbackReadRepository _userFeedbackReadRepository;
    private readonly IFeatureCategoryReadRepository _featureCategoryReadRepository;
    private readonly ILogger<GetAnalyzedFeedbacksQueryHandler> _logger;

    public GetAnalyzedFeedbacksQueryHandler(
        IUserFeedbackReadRepository userFeedbackReadRepository,
        IFeatureCategoryReadRepository featureCategoryReadRepository,
        ILogger<GetAnalyzedFeedbacksQueryHandler> logger)
    {
        _userFeedbackReadRepository = userFeedbackReadRepository;
        _featureCategoryReadRepository = featureCategoryReadRepository;
        _logger = logger;
    }

    public async Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> HandleAsync(GetAnalyzedFeedbacksQuery query, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Entering {MethodName} with query: {Query}", nameof(HandleAsync), query);

        ISet<FeatureCategoryReadModel> featureCategories = await GetFeatureCategoriesAsync(query);

        UserFeedbackFilter filter = CreateUserFeedbackFilter(featureCategories, query);

        PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>> feedbackResults =
            await _userFeedbackReadRepository.GetPagedListAsync(filter, query.PageNumber, query.PageSize);

        _logger.LogDebug("Exiting {MethodName} with {Count} results", nameof(HandleAsync), feedbackResults.Items.Count());
        return feedbackResults;
    }

    private async Task<ISet<FeatureCategoryReadModel>> GetFeatureCategoriesAsync(GetAnalyzedFeedbacksQuery query)
    {
        _logger.LogDebug("Entering {MethodName} with query: {Query}", nameof(GetFeatureCategoriesAsync), query);
        if (query.FeatureCategoryNames == null || query.FeatureCategoryNames.Any() == false)
        {
            _logger.LogDebug("Exiting {MethodName} with empty feature categories (no names provided)", nameof(GetFeatureCategoriesAsync));
            return new HashSet<FeatureCategoryReadModel>();
        }

        ISet<FeatureCategoryReadModel> result = (await _featureCategoryReadRepository.GetFeatureCategoriesByNamesAsync(query.FeatureCategoryNames))
            .ToHashSet();
        _logger.LogDebug("Exiting {MethodName} with {Count} feature categories", nameof(GetFeatureCategoriesAsync), result.Count);
        return result;
    }

    private static UserFeedbackFilter CreateUserFeedbackFilter(ISet<FeatureCategoryReadModel> featureCategories, GetAnalyzedFeedbacksQuery query)
    {
        return new UserFeedbackFilter
        {
            FeedbackCategories = query.FeedbackCategories,
            FeatureCategoryIds = featureCategories.Select(c => c.Id),
            SortBy = query.SortBy,
            SortAscending = query.SortOrder == SortOrder.Asc,
            AnalysisStatus = AnalysisStatus.Analyzed
        };
    }
}
