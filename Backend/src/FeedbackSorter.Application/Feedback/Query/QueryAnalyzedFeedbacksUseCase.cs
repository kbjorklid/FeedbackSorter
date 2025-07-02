using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Repositories;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackReadRepository;
using FeedbackSorter.Core;
using FeedbackSorter.Core.FeatureCategories;
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
        AnalyzedFeedbackQueryParams filter = CreateQueryParams(query);

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

    private static AnalyzedFeedbackQueryParams CreateQueryParams(GetAnalyzedFeedbacksQuery query)
    {
        return new AnalyzedFeedbackQueryParams
        {
            FeedbackCategories = query.FeedbackCategories,
            FeatureCategoryNames = query.FeatureCategoryNames == null ? null : query.FeatureCategoryNames.Select(c => new FeatureCategoryName(c)),
            SortBy = query.SortBy,
            SortAscending = query.SortOrder == SortOrder.Asc,
            Sentiment = query.Sentiment,
        };
    }
}
