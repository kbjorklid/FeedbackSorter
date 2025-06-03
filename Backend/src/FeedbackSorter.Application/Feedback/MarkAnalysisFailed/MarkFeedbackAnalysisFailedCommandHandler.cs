using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.MarkAnalysisFailed;

public class MarkFeedbackAnalysisFailedCommandHandler(
    IUserFeedbackRepository userFeedbackRepository,
    ITimeProvider timeProvider,
    ILogger<MarkFeedbackAnalysisFailedCommandHandler> logger)
{
    private readonly IUserFeedbackRepository _userFeedbackRepository = userFeedbackRepository;
    private readonly ITimeProvider _timeProvider = timeProvider;
    private readonly ILogger<MarkFeedbackAnalysisFailedCommandHandler> _logger = logger;

    public async Task<Result<UserFeedback>> Handle(MarkFeedbackAnalysisFailedCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Entering {MethodName} with request: {Request}", nameof(Handle), request);
        ArgumentNullException.ThrowIfNull(request);

        Result<UserFeedback> userFeedbackResult = await _userFeedbackRepository.GetByIdAsync(request.FeedbackId);

        if (userFeedbackResult.IsFailure)
        {
            _logger.LogDebug("Exiting {MethodName} with failure: Feedback with ID {FeedbackId} not found.", nameof(Handle), request.FeedbackId.Value);
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

        Result<UserFeedback> updateResult = await _userFeedbackRepository.UpdateAsync(userFeedback);
        _logger.LogDebug("Exiting {MethodName} with result: {Result}", nameof(Handle), updateResult);
        return updateResult;
    }
}
