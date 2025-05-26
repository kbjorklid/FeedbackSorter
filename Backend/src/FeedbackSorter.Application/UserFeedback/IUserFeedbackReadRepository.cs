using FeedbackSorter.Application.UserFeedback.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FeedbackSorter.Application.UserFeedback;

public interface IUserFeedbackReadRepository
{
    Task<IEnumerable<AnalyzedFeedbackReadModel>> GetPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize);
    Task<IEnumerable<FailedToAnalyzeFeedbackReadModel>> GetFailedAnalysisPagedListAsync(FailedUserFeedbackFilter filter, int pageNumber, int pageSize);
}
