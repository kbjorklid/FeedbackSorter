using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.Feedback.MarkAnalysisFailed;
using FeedbackSorter.Application.Feedback.MarkAnalyzed;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback.AnalyzeFeedback;

public class AnalyzeFeedbackCommandHandler(
    IUserFeedbackRepository userFeedbackRepository,
    IFeatureCategoryReadRepository featureCategoryReadRepository,
    ILlmFeedbackAnalyzer llmFeedbackAnalyzer,
    MarkFeedbackAnalyzedCommandHandler markFeedbackAnalyzedCommandHandler,
    MarkFeedbackAnalysisFailedCommandHandler markFeedbackAnalysisFailedCommandHandler) : IAnalyzeFeedbackCommandHandler
{
    private readonly IUserFeedbackRepository _userFeedbackRepository = userFeedbackRepository;
    private readonly IFeatureCategoryReadRepository _featureCategoryReadRepository = featureCategoryReadRepository;
    private readonly ILlmFeedbackAnalyzer _llmFeedbackAnalyzer = llmFeedbackAnalyzer;
    private readonly MarkFeedbackAnalyzedCommandHandler _markFeedbackAnalyzedCommandHandler = markFeedbackAnalyzedCommandHandler;
    private readonly MarkFeedbackAnalysisFailedCommandHandler _markFeedbackAnalysisFailedCommandHandler = markFeedbackAnalysisFailedCommandHandler;

    public async Task<Result> Handle(AnalyzeFeedbackCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Result<UserFeedback> feedbackResult = await _userFeedbackRepository.GetByIdAsync(command.FeedbackId);
        if (feedbackResult.IsFailure)
        {
            return Result.Failure(feedbackResult.Error);
        }
        UserFeedback userFeedback = feedbackResult.Value;

        userFeedback.StartProcessing();
        Result<UserFeedback> updateResult = await _userFeedbackRepository.UpdateAsync(userFeedback);
        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error);
        }

        IEnumerable<FeatureCategoryReadModel> existingFeatureCategories = await _featureCategoryReadRepository.GetAllAsync();

        LlmAnalysisResult llmAnalysisResult = await _llmFeedbackAnalyzer.AnalyzeFeedback(
            userFeedback.Text,
            existingFeatureCategories);

        if (llmAnalysisResult.IsSuccess)
        {
            MarkFeedbackAnalyzedCommand markAnalyzedCommand = new(command.FeedbackId, llmAnalysisResult);
            return await _markFeedbackAnalyzedCommandHandler.Handle(markAnalyzedCommand);
        }
        else
        {
            MarkFeedbackAnalysisFailedCommand markFailedCommand = new(command.FeedbackId, llmAnalysisResult);
            return await _markFeedbackAnalysisFailedCommandHandler.Handle(markFailedCommand, cancellationToken);
        }
    }
}
