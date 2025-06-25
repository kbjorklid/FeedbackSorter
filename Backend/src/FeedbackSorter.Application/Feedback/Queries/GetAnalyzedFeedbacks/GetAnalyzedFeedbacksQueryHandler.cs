using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackReadRepository;
using FeedbackSorter.Core;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.Queries.GetAnalyzedFeedbacks;

public class GetAnalyzedFeedbacksQueryHandler(
    IUserFeedbackReadRepository userFeedbackReadRepository,
    IFeatureCategoryReadRepository featureCategoryReadRepository,
    ILogger<GetAnalyzedFeedbacksQueryHandler> logger)
{
    private readonly ILogger<GetAnalyzedFeedbacksQueryHandler> _logger = logger;

    public async Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> HandleAsync(GetAnalyzedFeedbacksQuery query, CancellationToken cancellationToken)
    {

        ISet<FeatureCategoryReadModel> featureCategories = await GetFeatureCategoriesAsync(query);

        UserFeedbackFilter filter = CreateUserFeedbackFilter(featureCategories, query);

        PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>> feedbackResults =
            await userFeedbackReadRepository.GetPagedListAsync(filter, query.PageNumber, query.PageSize);

        return feedbackResults;
    }

    private async Task<ISet<FeatureCategoryReadModel>> GetFeatureCategoriesAsync(GetAnalyzedFeedbacksQuery query)
    {
        if (query.FeatureCategoryNames == null || query.FeatureCategoryNames.Any() == false)
        {
            return new HashSet<FeatureCategoryReadModel>();
        }

        ISet<FeatureCategoryReadModel> result = (await featureCategoryReadRepository.GetFeatureCategoriesByNamesAsync(query.FeatureCategoryNames))
            .ToHashSet();
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
