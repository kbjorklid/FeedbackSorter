using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.Analysis;

public class MarkFeedbackAnalysisFailedUseCase(
    IUserFeedbackRepository userFeedbackRepository,
    ITimeProvider timeProvider,
    ILogger<MarkFeedbackAnalysisFailedUseCase> logger)
{
    public async Task<Result<UserFeedback>> Handle(
        FeedbackId feedbackId, LlmAnalysisResult llmAnalysisResult, CancellationToken cancellationToken)
    {

        Result<UserFeedback> userFeedbackResult = await userFeedbackRepository.GetByIdAsync(feedbackId);

        if (userFeedbackResult.IsFailure) 
            return Result<UserFeedback>.Failure($"Feedback with ID {feedbackId} not found.");

        UserFeedback userFeedback = userFeedbackResult.Value;
        LlmAnalysisFailure llmAnalysisFailure = llmAnalysisResult.Failure!;
        var failureDetails = new AnalysisFailureDetails(
            llmAnalysisFailure.Reason,
            llmAnalysisFailure.Error,
            new Timestamp(timeProvider.UtcNow),
            userFeedback.RetryCount + 1
        );

        userFeedback.MarkAsFailed(failureDetails);

        Result<UserFeedback> updateResult = await userFeedbackRepository.UpdateAsync(userFeedback);
        return updateResult;
    }
}
