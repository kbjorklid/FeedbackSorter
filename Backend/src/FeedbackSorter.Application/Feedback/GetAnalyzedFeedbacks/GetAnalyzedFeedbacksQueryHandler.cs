using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Core;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback.GetAnalyzedFeedbacks;

public class GetAnalyzedFeedbacksQueryHandler
{
    private readonly IUserFeedbackReadRepository _userFeedbackReadRepository;
    private readonly IFeatureCategoryReadRepository _featureCategoryReadRepository;

    public GetAnalyzedFeedbacksQueryHandler(IUserFeedbackReadRepository userFeedbackReadRepository, IFeatureCategoryReadRepository featureCategoryReadRepository)
    {
        _userFeedbackReadRepository = userFeedbackReadRepository;
        _featureCategoryReadRepository = featureCategoryReadRepository;
    }

    public async Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> HandleAsync(GetAnalyzedFeedbacksQuery query, CancellationToken cancellationToken)
    {
        ISet<FeatureCategoryReadModel> featureCategories = await GetFeatureCategoriesAsync(query);

        UserFeedbackFilter filter = CreateUserFeedbackFilter(featureCategories, query);

        PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>> feedbackResults =
            await _userFeedbackReadRepository.GetPagedListAsync(filter, query.PageNumber, query.PageSize);

        return feedbackResults;
    }

    private async Task<ISet<FeatureCategoryReadModel>> GetFeatureCategoriesAsync(GetAnalyzedFeedbacksQuery query)
    {
        if (query.FeatureCategoryNames == null || query.FeatureCategoryNames.Any() == false)
        {
            return new HashSet<FeatureCategoryReadModel>();
        }

        return (await _featureCategoryReadRepository.GetFeatureCategoriesByNamesAsync(query.FeatureCategoryNames))
            .ToHashSet();
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
