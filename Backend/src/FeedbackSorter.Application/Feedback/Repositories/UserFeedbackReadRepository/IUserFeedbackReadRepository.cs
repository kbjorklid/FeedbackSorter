using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Queries.GetAnalyzedFeedbacks;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback.Repositories.UserFeedbackReadRepository;

public interface IUserFeedbackReadRepository
{
    Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> GetPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize);
    Task<List<FailedToAnalyzeFeedbackReadModel>> GetFailedAnalysisPagedListAsync(FailedToAnalyzeUserFeedbackFilter filter, int pageNumber, int pageSize);
}
