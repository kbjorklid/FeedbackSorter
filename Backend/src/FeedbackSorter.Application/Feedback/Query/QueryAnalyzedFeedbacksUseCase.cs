using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Queries.GetAnalyzedFeedbacks;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackReadRepository;
using FeedbackSorter.Core;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.Query;

public class QueryAnalyzedFeedbacksUseCase(
    IUserFeedbackReadRepository userFeedbackReadRepository,
    IFeatureCategoryReadRepository featureCategoryReadRepository,
    ILogger<QueryAnalyzedFeedbacksUseCase> logger)
{
    private readonly ILogger<QueryAnalyzedFeedbacksUseCase> _logger = logger;

    public async Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> HandleAsync(GetAnalyzedFeedbacksQuery query, CancellationToken cancellationToken)
    {

        ISet<FeatureCategoryReadModel>? featureCategories = await GetFeatureCategoriesAsync(query);

        AnalyzedFeedbackQueryParams filter = CreateQueryParams(featureCategories, query);

        PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>> feedbackResults =
            await userFeedbackReadRepository.GetPagedListAsync(filter, query.PageNumber, query.PageSize);

        return feedbackResults;
    }

    private async Task<ISet<FeatureCategoryReadModel>?> GetFeatureCategoriesAsync(GetAnalyzedFeedbacksQuery query)
    {
        if (query.FeatureCategoryNames == null || query.FeatureCategoryNames.Any() == false)
            return null;
    
        ISet<FeatureCategoryReadModel> result = (await featureCategoryReadRepository.GetFeatureCategoriesByNamesAsync(query.FeatureCategoryNames))
            .ToHashSet();
        return result;
    }

    private static AnalyzedFeedbackQueryParams CreateQueryParams(ISet<FeatureCategoryReadModel>? featureCategories, GetAnalyzedFeedbacksQuery query)
    {
        return new AnalyzedFeedbackQueryParams
        {
            FeedbackCategories = query.FeedbackCategories,
            FeatureCategoryIds = featureCategories == null ? null : featureCategories.Select(c => c.Id),
            SortBy = query.SortBy,
            SortAscending = query.SortOrder == SortOrder.Asc
        };
    }
}
