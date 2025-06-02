using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
namespace FeedbackSorter.Application.Feedback;

public interface IUserFeedbackRepository
{
    Task<Result<UserFeedback>> GetByIdAsync(FeedbackId id);
    Task<Result<UserFeedback>> AddAsync(UserFeedback userFeedback);
    Task<Result<UserFeedback>> UpdateAsync(UserFeedback userFeedback);
}
