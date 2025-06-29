using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.Analysis;

public class AnalyzeFeedbackUseCase(
    IUserFeedbackRepository userFeedbackRepository,
    IFeatureCategoryReadRepository featureCategoryReadRepository,
    ILlmFeedbackAnalyzer llmFeedbackAnalyzer,
    MarkFeedbackAnalyzedUseCase markFeedbackAnalyzedUseCase,
    MarkFeedbackAnalysisFailedUseCase markFeedbackAnalysisFailedUseCase,
    ILogger<AnalyzeFeedbackUseCase> logger)
{
    public async Task<Result> Execute(FeedbackId feedbackId, CancellationToken cancellationToken = default)
    {

        Result<UserFeedback> feedbackResult = await userFeedbackRepository.GetByIdAsync(feedbackId);
        if (feedbackResult.IsFailure)
            return Result.Failure(feedbackResult.Error);

        UserFeedback userFeedback = feedbackResult.Value;
        if (!IsInAnalyzableState(userFeedback))
        {
            return Result.Failure(
                $"User Feedback with ID {feedbackId} is not in correct state " +
                $"(state is: {userFeedback.AnalysisStatus})");
        }

        userFeedback.StartProcessing();
        Result<UserFeedback> updateResult = await userFeedbackRepository.UpdateAsync(userFeedback);
        if (updateResult.IsFailure) return Result.Failure(updateResult.Error);

        LlmAnalysisResult llmAnalysisResult = await AnalyzeFeedbackWithLlm(userFeedback);
        if (llmAnalysisResult.IsSuccess) return await markFeedbackAnalyzedUseCase.Handle(feedbackId, llmAnalysisResult);
        return await markFeedbackAnalysisFailedUseCase.Handle(feedbackId, llmAnalysisResult, cancellationToken);
    }

    private static bool IsInAnalyzableState(UserFeedback userFeedback)
    {
        return userFeedback.AnalysisStatus is (AnalysisStatus.WaitingForAnalysis or AnalysisStatus.AnalysisFailed);
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
