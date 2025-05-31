using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Core;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.UserFeedback.GetAnalyzedFeedbacks;

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
        ISet<FeatureCategoryReadModel> featureCategories;
        if (query.FeatureCategoryNames == null || query.FeatureCategoryNames.Any() == false)
        {
            featureCategories = new HashSet<FeatureCategoryReadModel>();
        }
        else
        {
            featureCategories =
                (await _featureCategoryReadRepository.GetFeatureCategoriesByNamesAsync(query.FeatureCategoryNames))
                .ToHashSet();
        }

        var filter = new UserFeedbackFilter
        {
            FeedbackCategories = query.FeedbackCategories,
            FeatureCategoryIds = featureCategories.Select(c => c.Id),
            SortBy = query.SortBy,
            SortAscending = query.SortOrder == SortOrder.Asc,
            AnalysisStatus = AnalysisStatus.Analyzed
        };

        PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>> feedbackResults =
            await _userFeedbackReadRepository.GetPagedListAsync(filter, query.PageNumber, query.PageSize);

        return feedbackResults;
    }
}
