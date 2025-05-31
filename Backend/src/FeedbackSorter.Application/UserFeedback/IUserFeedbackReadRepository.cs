using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.UserFeedback.GetAnalyzedFeedbacks;
using FeedbackSorter.Application.UserFeedback.Queries;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.UserFeedback;

public interface IUserFeedbackReadRepository
{
    Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> GetPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize);
    Task<List<FailedToAnalyzeFeedbackReadModel>> GetFailedAnalysisPagedListAsync(FailedToAnalyzeUserFeedbackFilter filter, int pageNumber, int pageSize);
}
