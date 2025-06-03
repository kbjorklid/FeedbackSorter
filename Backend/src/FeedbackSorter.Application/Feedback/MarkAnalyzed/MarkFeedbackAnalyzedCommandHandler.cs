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
        _logger.LogDebug("Entering {MethodName} with command: {Command}", nameof(Handle), command);
        ArgumentNullException.ThrowIfNull(command);
        if (command.LlmAnalysisResult.IsSuccess == false)
        {
            _logger.LogDebug("Exiting {MethodName} with failure: Asked to mark feedback as analyzed, but result indicates failure.", nameof(Handle));
            throw new InvalidOperationException("Asked to mark feedback as analyzed, but result indicates failure.");
        }

        Result<UserFeedback> feedbackResult = await _userFeedbackRepository.GetByIdAsync(command.UserFeedbackId);
        if (feedbackResult.IsFailure)
        {
            _logger.LogDebug("Exiting {MethodName} with failure: {Error}", nameof(Handle), feedbackResult.Error);
            return Result.Failure(feedbackResult.Error);
        }
        UserFeedback userFeedback = feedbackResult.Value;

        ISet<string> featureCategoryNames = command.LlmAnalysisResult.Success!.FeatureCategoryNames;

        Result<ISet<FeatureCategory>> featureCategoriesResult = await GetOrCreateFeatureCategoriesAsync(featureCategoryNames);
        if (featureCategoriesResult.IsFailure)
        {
            _logger.LogDebug("Exiting {MethodName} with failure: {Error}", nameof(Handle), featureCategoriesResult.Error);
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

        _logger.LogDebug("Exiting {MethodName} with result: {Result}", nameof(Handle), saveResult.IsSuccess ? Result.Success() : Result.Failure(saveResult.Error));
        return saveResult.IsSuccess ? Result.Success() : Result.Failure(saveResult.Error);
    }

    private async Task<Result<ISet<FeatureCategory>>> GetOrCreateFeatureCategoriesAsync(ISet<string> featureCategoryNames)
    {
        _logger.LogDebug("Entering {MethodName} with featureCategoryNames: {FeatureCategoryNames}", nameof(GetOrCreateFeatureCategoriesAsync), featureCategoryNames);
        ISet<FeatureCategory> existingFeatureCategories = await _featureCategoryRepository.GetByNamesAsync(featureCategoryNames);
        var featureCategories = new HashSet<FeatureCategory>(existingFeatureCategories);

        foreach (string featureCategoryName in featureCategoryNames)
        {
            if (!existingFeatureCategories.Any(fc => fc.Name.Value == featureCategoryName))
            {
                _logger.LogDebug("Creating new feature category: {FeatureCategoryName}", featureCategoryName);
                var newFeatureCategory = new FeatureCategory(new FeatureCategoryId(Guid.NewGuid()), new FeatureCategoryName(featureCategoryName), new Timestamp());
                Result<FeatureCategory> addResult = await _featureCategoryRepository.AddAsync(newFeatureCategory);
                if (addResult.IsSuccess)
                {
                    featureCategories.Add(newFeatureCategory);
                }
                else
                {
                    _logger.LogDebug("Failed to add new feature category: {Error}", addResult.Error);
                    return Result<ISet<FeatureCategory>>.Failure($"Failed to add new feature category: {addResult.Error}");
                }
            }
        }
        _logger.LogDebug("Exiting {MethodName} with {Count} feature categories", nameof(GetOrCreateFeatureCategoriesAsync), featureCategories.Count);
        return Result<ISet<FeatureCategory>>.Success(featureCategories);
    }
}
