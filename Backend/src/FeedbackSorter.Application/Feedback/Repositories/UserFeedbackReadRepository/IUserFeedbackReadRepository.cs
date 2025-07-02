using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback.Repositories.UserFeedbackReadRepository;

public interface IUserFeedbackReadRepository
{
    Task<PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>> GetPagedListAsync(AnalyzedFeedbackQueryParams filter, int pageNumber, int pageSize);
    Task<PagedResult<FailedToAnalyzeFeedbackReadModel>> GetFailedAnalysisPagedListAsync(int pageNumber, int pageSize);
}
