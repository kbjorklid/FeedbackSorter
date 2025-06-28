using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;

public interface IUserFeedbackRepository
{
    Task<Result<UserFeedback>> GetByIdAsync(FeedbackId id);
    Task<UserFeedback> AddAsync(UserFeedback userFeedback);
    Task<Result<UserFeedback>> UpdateAsync(UserFeedback userFeedback);
    Task<IList<UserFeedback>> QueryAsync(UserFeedbackQuery query);

}

