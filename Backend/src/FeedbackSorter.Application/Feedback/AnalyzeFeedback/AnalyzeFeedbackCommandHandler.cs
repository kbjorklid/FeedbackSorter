using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.Feedback.MarkAnalysisFailed;
using FeedbackSorter.Application.Feedback.MarkAnalyzed;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.AnalyzeFeedback;

public class AnalyzeFeedbackCommandHandler(
    IUserFeedbackRepository userFeedbackRepository,
    IFeatureCategoryReadRepository featureCategoryReadRepository,
    ILlmFeedbackAnalyzer llmFeedbackAnalyzer,
    MarkFeedbackAnalyzedCommandHandler markFeedbackAnalyzedCommandHandler,
    MarkFeedbackAnalysisFailedCommandHandler markFeedbackAnalysisFailedCommandHandler,
    ILogger<AnalyzeFeedbackCommandHandler> logger) : IAnalyzeFeedbackCommandHandler
{
    private readonly IUserFeedbackRepository _userFeedbackRepository = userFeedbackRepository;
    private readonly IFeatureCategoryReadRepository _featureCategoryReadRepository = featureCategoryReadRepository;
    private readonly ILlmFeedbackAnalyzer _llmFeedbackAnalyzer = llmFeedbackAnalyzer;
    private readonly MarkFeedbackAnalyzedCommandHandler _markFeedbackAnalyzedCommandHandler = markFeedbackAnalyzedCommandHandler;
    private readonly MarkFeedbackAnalysisFailedCommandHandler _markFeedbackAnalysisFailedCommandHandler = markFeedbackAnalysisFailedCommandHandler;
    private readonly ILogger<AnalyzeFeedbackCommandHandler> _logger = logger;

    public async Task<Result> Handle(AnalyzeFeedbackCommand command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Entering {MethodName} with command: {Command}", nameof(Handle), command);
        ArgumentNullException.ThrowIfNull(command);
        Result<UserFeedback> feedbackResult = await _userFeedbackRepository.GetByIdAsync(command.FeedbackId);
        if (feedbackResult.IsFailure)
        {
            _logger.LogDebug("Exiting {MethodName} with failure: {Error}", nameof(Handle), feedbackResult.Error);
            return Result.Failure(feedbackResult.Error);
        }
        UserFeedback userFeedback = feedbackResult.Value;
        userFeedback.StartProcessing();
        Result<UserFeedback> updateResult = await _userFeedbackRepository.UpdateAsync(userFeedback);
        if (updateResult.IsFailure)
        {
            _logger.LogDebug("Exiting {MethodName} with failure: {Error}", nameof(Handle), updateResult.Error);
            return Result.Failure(updateResult.Error);
        }
        IEnumerable<FeatureCategoryReadModel> existingFeatureCategories = await _featureCategoryReadRepository.GetAllAsync();

        LlmAnalysisResult llmAnalysisResult = await _llmFeedbackAnalyzer.AnalyzeFeedback(
            userFeedback.Text,
            existingFeatureCategories);

        if (llmAnalysisResult.IsSuccess)
        {
            MarkFeedbackAnalyzedCommand markAnalyzedCommand = new(command.FeedbackId, llmAnalysisResult);
            Result result = await _markFeedbackAnalyzedCommandHandler.Handle(markAnalyzedCommand);
            _logger.LogDebug("Exiting {MethodName} with success. MarkFeedbackAnalyzedCommandHandler result: {Result}", nameof(Handle), result);
            return result;
        }
        else
        {
            MarkFeedbackAnalysisFailedCommand markFailedCommand = new(command.FeedbackId, llmAnalysisResult);
            Result result = await _markFeedbackAnalysisFailedCommandHandler.Handle(markFailedCommand, cancellationToken);
            _logger.LogDebug("Exiting {MethodName} with failure. MarkFeedbackAnalysisFailedCommandHandler result: {Result}", nameof(Handle), result);
            return result;
        }
    }
}
