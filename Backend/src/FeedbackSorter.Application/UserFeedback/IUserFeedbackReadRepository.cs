using FeedbackSorter.Application.UserFeedback.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FeedbackSorter.Application.UserFeedback;

public interface IUserFeedbackReadRepository
{
    Task<IEnumerable<UserFeedbackReadModel>> GetPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize);
    Task<IEnumerable<UserFeedbackReadModel>> GetFailedAnalysisPagedListAsync(UserFeedbackFilter filter, int pageNumber, int pageSize);
}
