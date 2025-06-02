using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.Feedback.GetAnalyzedFeedbacks;
using FeedbackSorter.Application.Feedback.Queries;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback;

public interface IUserFeedbackReadRepository
{
    Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> GetPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize);
    Task<List<FailedToAnalyzeFeedbackReadModel>> GetFailedAnalysisPagedListAsync(FailedToAnalyzeUserFeedbackFilter filter, int pageNumber, int pageSize);
}
