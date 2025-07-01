
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback.Delete;

public class DeleteFeedbackUseCase(IUserFeedbackRepository userFeedbackRepository)
{
    public async Task<Result<bool>> Handle(FeedbackId id, CancellationToken cancellationToken)
    {
        var result = await userFeedbackRepository.DeleteAsync(id);
        return Result<bool>.Success(result);
    }
}
