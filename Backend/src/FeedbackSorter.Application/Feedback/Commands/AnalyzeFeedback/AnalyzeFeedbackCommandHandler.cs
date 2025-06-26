using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Commands.MarkAnalysisFailed;
using FeedbackSorter.Application.Feedback.Commands.MarkAnalyzed;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.Commands.AnalyzeFeedback;

public class AnalyzeFeedbackCommandHandler(
    IUserFeedbackRepository userFeedbackRepository,
    IFeatureCategoryReadRepository featureCategoryReadRepository,
    ILlmFeedbackAnalyzer llmFeedbackAnalyzer,
    MarkFeedbackAnalyzedCommandHandler markFeedbackAnalyzedCommandHandler,
    MarkFeedbackAnalysisFailedCommandHandler markFeedbackAnalysisFailedCommandHandler,
    ILogger<AnalyzeFeedbackCommandHandler> logger) : IAnalyzeFeedbackCommandHandler
{
    public async Task<Result> Handle(AnalyzeFeedbackCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        
        Result<UserFeedback> feedbackResult = await userFeedbackRepository.GetByIdAsync(command.FeedbackId);
        if (feedbackResult.IsFailure) return Result.Failure(feedbackResult.Error);
        
        UserFeedback userFeedback = feedbackResult.Value;
        userFeedback.StartProcessing();
        Result<UserFeedback> updateResult = await userFeedbackRepository.UpdateAsync(userFeedback);
        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error);
        }
        
        LlmAnalysisResult llmAnalysisResult = await AnalyzeFeedbackWithLlm(userFeedback);

        if (llmAnalysisResult.IsSuccess)
        {
            MarkFeedbackSuccessfullyAnalyzedCommand markSuccessfullyAnalyzedCommand = 
                new(command.FeedbackId, llmAnalysisResult);
            return await markFeedbackAnalyzedCommandHandler.Handle(markSuccessfullyAnalyzedCommand);
        }

        MarkFeedbackAnalysisFailedCommand markFailedCommand = new(command.FeedbackId, llmAnalysisResult);
        return await markFeedbackAnalysisFailedCommandHandler.Handle(markFailedCommand, cancellationToken);
    }

    private async Task<LlmAnalysisResult> AnalyzeFeedbackWithLlm(UserFeedback userFeedback)
    {
        IEnumerable<FeatureCategoryReadModel> existingFeatureCategories = 
            await featureCategoryReadRepository.GetAllAsync();

        LlmAnalysisResult llmAnalysisResult = await llmFeedbackAnalyzer.AnalyzeFeedback(
            userFeedback.Text,
            existingFeatureCategories);
        return llmAnalysisResult;
    }
}
