using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using CoreUserFeedback = FeedbackSorter.Core.Feedback.UserFeedback;

namespace FeedbackSorter.Application.UserFeedback;

public interface IUserFeedbackRepository
{
    Task<Result<CoreUserFeedback>> GetByIdAsync(FeedbackId id);
    Task<Result<CoreUserFeedback>> AddAsync(CoreUserFeedback userFeedback);
    Task<Result<CoreUserFeedback>> UpdateAsync(CoreUserFeedback userFeedback);
}
