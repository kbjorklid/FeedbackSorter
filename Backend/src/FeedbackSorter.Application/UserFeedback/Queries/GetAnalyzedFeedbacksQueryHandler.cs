using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.UserFeedback.Queries;

public class GetAnalyzedFeedbacksQueryHandler
{
    private readonly IUserFeedbackReadRepository _userFeedbackReadRepository;
    private readonly IFeatureCategoryReadRepository _featureCategoryReadRepository;

    public GetAnalyzedFeedbacksQueryHandler(IUserFeedbackReadRepository userFeedbackReadRepository, IFeatureCategoryReadRepository featureCategoryReadRepository)
    {
        _userFeedbackReadRepository = userFeedbackReadRepository;
        _featureCategoryReadRepository = featureCategoryReadRepository;
    }

    public async Task<PagedResult<AnalyzedFeedbackReadModel>> HandleAsync(GetAnalyzedFeedbacksQuery query, CancellationToken cancellationToken)
    {
        IEnumerable<Guid>? featureCategoryIds = null;
        if (query.FeatureCategoryNames != null && query.FeatureCategoryNames.Any())
        {
            IEnumerable<FeatureCategories.Queries.FeatureCategoryReadModel> featureCategories = await _featureCategoryReadRepository.GetFeatureCategoriesByNamesAsync(query.FeatureCategoryNames);
            featureCategoryIds = featureCategories.Select(fc => fc.Id.Value);
        }

        var filter = new UserFeedbackFilter
        {
            FeedbackCategories = query.FeedbackCategories,
            FeatureCategoryIds = featureCategoryIds?.Select(id => new FeatureCategoryId(id)),
            SortBy = query.SortBy,
            SortAscending = query.SortOrder == SortOrder.Asc,
            AnalysisStatus = AnalysisStatus.Analyzed
        };

        PagedResult<AnalyzedFeedbackReadModel> pagedResult = await _userFeedbackReadRepository.GetPagedListAsync(filter, query.PageNumber, query.PageSize);

        return pagedResult;
    }
}
