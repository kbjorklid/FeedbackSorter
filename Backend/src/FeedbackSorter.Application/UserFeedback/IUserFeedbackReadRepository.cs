using FeedbackSorter.Application.UserFeedback.Queries;

namespace FeedbackSorter.Application.UserFeedback;

public interface IUserFeedbackReadRepository
{
    Task<List<AnalyzedFeedbackReadModel>> GetPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize);
    Task<List<FailedToAnalyzeFeedbackReadModel>> GetFailedAnalysisPagedListAsync(FailedToAnalyzeUserFeedbackFilter filter, int pageNumber, int pageSize);
}
