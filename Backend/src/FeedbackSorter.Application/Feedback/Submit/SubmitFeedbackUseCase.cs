using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Core.Feedback;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.Submit;

public class SubmitFeedbackUseCase(
    IUserFeedbackRepository userFeedbackRepository,
    ILogger<SubmitFeedbackUseCase> logger)
{

    public async Task<FeedbackId> HandleAsync(FeedbackText text)
    {
        var feedbackId = FeedbackId.New();
        var initialUserFeedback = new UserFeedback(feedbackId, text);
        await userFeedbackRepository.AddAsync(initialUserFeedback);
        return feedbackId;
    }
}
