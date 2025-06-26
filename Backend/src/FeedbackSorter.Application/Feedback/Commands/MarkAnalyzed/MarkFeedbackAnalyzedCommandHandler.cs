using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Commands.CreateOrGetFeatureCategories;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.Commands.MarkAnalyzed;

public class MarkFeedbackAnalyzedCommandHandler(
    IUserFeedbackRepository userFeedbackRepository,
    CreateOrGetFeatureCategoriesCommandHandler createOrGetFeatureCategoriesCommandHandler,
    ILogger<MarkFeedbackAnalyzedCommandHandler> logger)
{

    public async Task<Result> Handle(MarkFeedbackSuccessfullyAnalyzedCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.LlmAnalysisResult.IsSuccess == false)
        {
            throw new InvalidOperationException("Asked to mark feedback as analyzed, but result indicates failure.");
        }

        Result<UserFeedback> feedbackResult = await userFeedbackRepository.GetByIdAsync(command.UserFeedbackId);
        if (feedbackResult.IsFailure)
        {
            return Result.Failure(feedbackResult.Error);
        }
        UserFeedback userFeedback = feedbackResult.Value;
        ISet<string> featureCategoryNames = command.LlmAnalysisResult.Success!.FeatureCategoryNames;

        var featureCategoriesCommand = new CreateOrGetFeatureCategoriesCommand(featureCategoryNames);
        Result<ISet<FeatureCategory>> featureCategoriesResult = 
            await createOrGetFeatureCategoriesCommandHandler.Execute(featureCategoriesCommand);
        
        if (featureCategoriesResult.IsFailure)
            return Result.Failure(featureCategoriesResult.Error);

        LLM.LlmAnalysisSuccess results = command.LlmAnalysisResult.Success!;
        var feedbackAnalysisResult = new FeedbackAnalysisResult(
            results.Title,
            results.Sentiment,
            results.FeedbackCategories,
            featureCategoriesResult.Value,
            command.LlmAnalysisResult.AnalyzedAt
        );

        userFeedback.MarkAsAnalyzed(feedbackAnalysisResult);
        Result<UserFeedback> saveResult = await userFeedbackRepository.UpdateAsync(userFeedback);
        return saveResult.IsSuccess ? Result.Success() : Result.Failure(saveResult.Error);
    }
}
