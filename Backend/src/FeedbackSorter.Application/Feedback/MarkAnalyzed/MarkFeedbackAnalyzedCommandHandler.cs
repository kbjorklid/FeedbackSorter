using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.MarkAnalyzed;

public class MarkFeedbackAnalyzedCommandHandler(
    IUserFeedbackRepository userFeedbackRepository,
    IFeatureCategoryRepository featureCategoryRepository,
    ILogger<MarkFeedbackAnalyzedCommandHandler> logger)
{
    private readonly IUserFeedbackRepository _userFeedbackRepository = userFeedbackRepository;
    private readonly IFeatureCategoryRepository _featureCategoryRepository = featureCategoryRepository;
    private readonly ILogger<MarkFeedbackAnalyzedCommandHandler> _logger = logger;

    public async Task<Result> Handle(MarkFeedbackAnalyzedCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.LlmAnalysisResult.IsSuccess == false)
        {
            throw new InvalidOperationException("Asked to mark feedback as analyzed, but result indicates failure.");
        }

        Result<UserFeedback> feedbackResult = await _userFeedbackRepository.GetByIdAsync(command.UserFeedbackId);
        if (feedbackResult.IsFailure)
        {
            return Result.Failure(feedbackResult.Error);
        }
        UserFeedback userFeedback = feedbackResult.Value;

        ISet<string> featureCategoryNames = command.LlmAnalysisResult.Success!.FeatureCategoryNames;

        Result<ISet<FeatureCategory>> featureCategoriesResult = await GetOrCreateFeatureCategoriesAsync(featureCategoryNames);
        if (featureCategoriesResult.IsFailure)
        {
            return Result.Failure(featureCategoriesResult.Error);
        }

        LLM.LlmAnalysisSuccess results = command.LlmAnalysisResult.Success!;
        var feedbackAnalysisResult = new FeedbackAnalysisResult(
            results.Title,
            results.Sentiment,
            results.FeedbackCategories,
            featureCategoriesResult.Value,
            command.LlmAnalysisResult.AnalyzedAt
        );

        userFeedback.MarkAsAnalyzed(feedbackAnalysisResult);

        Result<UserFeedback> saveResult = await _userFeedbackRepository.UpdateAsync(userFeedback);

        return saveResult.IsSuccess ? Result.Success() : Result.Failure(saveResult.Error);
    }

    private async Task<Result<ISet<FeatureCategory>>> GetOrCreateFeatureCategoriesAsync(ISet<string> featureCategoryNames)
    {
        ISet<FeatureCategory> existingFeatureCategories = await _featureCategoryRepository.GetByNamesAsync(featureCategoryNames);
        var featureCategories = new HashSet<FeatureCategory>(existingFeatureCategories);

        foreach (string featureCategoryName in featureCategoryNames)
        {
            if (!existingFeatureCategories.Any(fc => fc.Name.Value == featureCategoryName))
            {
                var newFeatureCategory = new FeatureCategory(new FeatureCategoryId(Guid.NewGuid()), new FeatureCategoryName(featureCategoryName), new Timestamp());
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
