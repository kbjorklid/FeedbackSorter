using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback.MarkAnalyzed;

public class MarkFeedbackAnalyzedCommandHandler(IUserFeedbackRepository userFeedbackRepository, IFeatureCategoryRepository featureCategoryRepository)
{
    private readonly IUserFeedbackRepository _userFeedbackRepository = userFeedbackRepository;
    private readonly IFeatureCategoryRepository _featureCategoryRepository = featureCategoryRepository;

    public async Task<Result> Handle(MarkFeedbackAnalyzedCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        Result<UserFeedback> feedbackResult = await _userFeedbackRepository.GetByIdAsync(command.UserFeedbackId);
        if (feedbackResult.IsFailure)
            return Result.Failure(feedbackResult.Error);

        UserFeedback userFeedback = feedbackResult.Value;

        Result<ISet<FeatureCategory>> featureCategoriesResult = await GetOrCreateFeatureCategoriesAsync(command.LlmAnalysisResult);
        if (featureCategoriesResult.IsFailure)
            return Result.Failure(featureCategoriesResult.Error);

        var feedbackAnalysisResult = new FeedbackAnalysisResult(
            command.LlmAnalysisResult.Title,
            command.LlmAnalysisResult.Sentiment,
            command.LlmAnalysisResult.FeedbackCategories,
            featureCategoriesResult.Value,
            command.LlmAnalysisResult.AnalyzedAt
        );

        userFeedback.MarkAsAnalyzed(feedbackAnalysisResult);

        Result<UserFeedback> saveResult = await _userFeedbackRepository.UpdateAsync(userFeedback);

        return saveResult.IsSuccess ? Result.Success() : Result.Failure(saveResult.Error);
    }

    private async Task<Result<ISet<FeatureCategory>>> GetOrCreateFeatureCategoriesAsync(LlmAnalysisResult llmAnalysisResult)
    {
        ISet<FeatureCategory> existingFeatureCategories = await _featureCategoryRepository.GetByNamesAsync(llmAnalysisResult.FeatureCategoryNames);
        var featureCategories = new HashSet<FeatureCategory>(existingFeatureCategories);

        foreach (string featureCategoryName in llmAnalysisResult.FeatureCategoryNames)
        {
            if (!existingFeatureCategories.Any(fc => fc.Name.Value == featureCategoryName))
            {
                var newFeatureCategory = new FeatureCategory(new FeatureCategoryId(Guid.NewGuid()), new FeatureCategoryName(featureCategoryName), llmAnalysisResult.AnalyzedAt);
                Result<FeatureCategory> addResult = await _featureCategoryRepository.AddAsync(newFeatureCategory);
                if (addResult.IsSuccess)
                {
                    featureCategories.Add(newFeatureCategory);
                }
                else
                {
                    return Result<ISet<FeatureCategory>>.Failure($"Failed to add new feature category: {addResult.Error}");
                }
            }
        }
        return Result<ISet<FeatureCategory>>.Success(featureCategories);
    }
}
