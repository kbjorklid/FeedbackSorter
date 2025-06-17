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
    private readonly ILogger<AnalyzeFeedbackCommandHandler> _logger = logger;

    public async Task<Result> Handle(AnalyzeFeedbackCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Result<UserFeedback> feedbackResult = await userFeedbackRepository.GetByIdAsync(command.FeedbackId);
        if (feedbackResult.IsFailure)
        {
            return Result.Failure(feedbackResult.Error);
        }
        UserFeedback userFeedback = feedbackResult.Value;
        userFeedback.StartProcessing();
        Result<UserFeedback> updateResult = await userFeedbackRepository.UpdateAsync(userFeedback);
        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error);
        }
        IEnumerable<FeatureCategoryReadModel> existingFeatureCategories = await featureCategoryReadRepository.GetAllAsync();

        LlmAnalysisResult llmAnalysisResult = await llmFeedbackAnalyzer.AnalyzeFeedback(
            userFeedback.Text,
            existingFeatureCategories);

        if (llmAnalysisResult.IsSuccess)
        {
            MarkFeedbackAnalyzedCommand markAnalyzedCommand = new(command.FeedbackId, llmAnalysisResult);
            Result result = await markFeedbackAnalyzedCommandHandler.Handle(markAnalyzedCommand);
            return result;
        }
        else
        {
            MarkFeedbackAnalysisFailedCommand markFailedCommand = new(command.FeedbackId, llmAnalysisResult);
            Result result = await markFeedbackAnalysisFailedCommandHandler.Handle(markFailedCommand, cancellationToken);
            return result;
        }
    }
}
