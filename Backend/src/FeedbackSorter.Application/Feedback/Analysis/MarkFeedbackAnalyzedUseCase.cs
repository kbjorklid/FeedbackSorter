using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Application.Notifications;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.Analysis;

public class MarkFeedbackAnalyzedUseCase(
    IUserFeedbackRepository userFeedbackRepository,
    CreateOrGetFeatureCategoriesUseCase createOrGetFeatureCategoriesUseCase,
    IFeedbackNotificationService notificationService,
    ILogger<MarkFeedbackAnalyzedUseCase> logger)
{

    public async Task<Result> Handle(FeedbackId userFeedbackId, LlmAnalysisResult llmAnalysisResult)
    {
        if (llmAnalysisResult.IsSuccess == false)
            throw new InvalidOperationException("Asked to mark feedback as analyzed, but result indicates failure.");

        Result<UserFeedback> feedbackResult = await userFeedbackRepository.GetByIdAsync(userFeedbackId);
        if (feedbackResult.IsFailure)
            return Result.Failure(feedbackResult.Error);

        UserFeedback userFeedback = feedbackResult.Value;
        LlmAnalysisSuccess results = llmAnalysisResult.Success!;
        ISet<string> featureCategoryNames = results.FeatureCategoryNames;

        ISet<FeatureCategory> featureCategories =
            await createOrGetFeatureCategoriesUseCase.Execute(featureCategoryNames);

        var feedbackAnalysisResult = new FeedbackAnalysisResult(
            results.Title,
            results.Sentiment,
            results.FeedbackCategories,
            featureCategories,
            llmAnalysisResult.AnalyzedAt
        );

        userFeedback.MarkAsAnalyzed(feedbackAnalysisResult);
        Result<UserFeedback> saveResult = await userFeedbackRepository.UpdateAsync(userFeedback);
        
        if (saveResult.IsFailure)
        {
            logger.LogWarning("Failed to save analyzed feedback {FeedbackId}: {Error}", userFeedbackId.Value, saveResult.Error);
            return Result.Failure(saveResult.Error);
        }
        
        await notificationService.NotifyFeedbackAnalyzed(userFeedbackId);
        
        return Result.Success();
    }
}
