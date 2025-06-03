using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback.MarkAnalysisFailed;

public class MarkFeedbackAnalysisFailedCommandHandler(IUserFeedbackRepository userFeedbackRepository, ITimeProvider timeProvider)
{
    private readonly IUserFeedbackRepository _userFeedbackRepository = userFeedbackRepository;
    private readonly ITimeProvider _timeProvider = timeProvider;

    public async Task<Result<UserFeedback>> Handle(MarkFeedbackAnalysisFailedCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<UserFeedback> userFeedbackResult = await _userFeedbackRepository.GetByIdAsync(request.FeedbackId);

        if (userFeedbackResult.IsFailure)
        {
            return Result<UserFeedback>.Failure($"Feedback with ID {request.FeedbackId.Value} not found.");
        }

        UserFeedback userFeedback = userFeedbackResult.Value;

        var failureDetails = new AnalysisFailureDetails(
            request.LlmAnalysisResult.Failure!.Reason,
            request.LlmAnalysisResult.Failure.Error,
            new Timestamp(_timeProvider.UtcNow),
            userFeedback.RetryCount + 1
        );

        userFeedback.MarkAsFailed(failureDetails);

        return await _userFeedbackRepository.UpdateAsync(userFeedback);
    }
}
