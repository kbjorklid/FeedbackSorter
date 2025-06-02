using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.SubmitNew;

public class SubmitFeedbackCommandHandler
{
    private readonly IUserFeedbackRepository _userFeedbackRepository;
    private readonly ILlmFeedbackAnalyzer _llmFeedbackAnalyzer;
    private readonly ITimeProvider _timeProvider;
    private readonly IFeatureCategoryReadRepository _featureCategoryReadRepository;
    private readonly IFeatureCategoryRepository _featureCategoryWriteRepository;
    private readonly ILogger<SubmitFeedbackCommandHandler> _logger;


    public SubmitFeedbackCommandHandler(
        IUserFeedbackRepository userFeedbackRepository,
        ILlmFeedbackAnalyzer llmFeedbackAnalyzer,
        ITimeProvider timeProvider,
        IFeatureCategoryReadRepository featureCategoryReadRepository,
        IFeatureCategoryRepository featureCategoryWriteRepository,
        ILogger<SubmitFeedbackCommandHandler> logger)
    {
        _userFeedbackRepository = userFeedbackRepository;
        _llmFeedbackAnalyzer = llmFeedbackAnalyzer;
        _timeProvider = timeProvider;
        _featureCategoryReadRepository = featureCategoryReadRepository;
        _featureCategoryWriteRepository = featureCategoryWriteRepository;
        _logger = logger;
    }

    public async Task<Result<FeedbackId>> HandleAsync(SubmitFeedbackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var feedbackId = FeedbackId.New();

        var initialUserFeedback = new FeedbackSorter.Core.Feedback.UserFeedback(
            feedbackId,
            new FeedbackText(command.Text)
        );
        Result<Core.Feedback.UserFeedback> addResult = await _userFeedbackRepository.AddAsync(initialUserFeedback);
        if (addResult.IsFailure)
        {
            _logger.LogError("Failed to add initial UserFeedback to repository: {Error}", addResult.Error);
            return Result<FeedbackId>.Failure($"Failed to add initial UserFeedback to repository: {addResult.Error}");
        }

        _ = Task.Run(async () => await AnalyzeAndSaveFeedbackAsync(addResult.Value));

        return Result<FeedbackId>.Success(feedbackId);
    }

    private async Task AnalyzeAndSaveFeedbackAsync(Core.Feedback.UserFeedback userFeedbackToAnalyze)
    {
        try
        {
            // Get existing feature categories
            IEnumerable<FeatureCategoryReadModel> existingFeatureCategoriesReadModels = await _featureCategoryReadRepository.GetAllAsync();
            var existingFeatureCategories = existingFeatureCategoriesReadModels
                .Select(fc => new FeatureCategoryReadModel(fc.Id, fc.Name))
                .ToList();

            Result<LlmAnalysisResult> llmAnalysis = await _llmFeedbackAnalyzer.AnalyzeFeedback(
                userFeedbackToAnalyze.Text,
                existingFeatureCategories
            );


            if (llmAnalysis.IsSuccess)
            {
                _logger.LogInformation("LLM analysis succeeded for feedback {FeedbackId}", userFeedbackToAnalyze.Id.Value);
                FeedbackAnalysisResult analysisResult = await BuildAnalysisResult(llmAnalysis.Value);
                userFeedbackToAnalyze.MarkAsAnalyzed(analysisResult);
                _logger.LogDebug("Analysis result for feedback {FeedbackId}: {AnalysisResult}", userFeedbackToAnalyze.Id.Value, userFeedbackToAnalyze.AnalysisResult);
                _ = await _userFeedbackRepository.UpdateAsync(userFeedbackToAnalyze);
            }
            else
            {
                // LLM analysis failed
                FailureReason failureReason = MapLLMErrorToFailureReason(llmAnalysis.Error);
                var failureDetails = new AnalysisFailureDetails(
                    failureReason,
                    llmAnalysis.Error,
                    new Timestamp(_timeProvider),
                    userFeedbackToAnalyze.RetryCount + 1 // Increment retry count for this failure
                );

                userFeedbackToAnalyze.MarkAsFailed(failureDetails);
                _ = await _userFeedbackRepository.UpdateAsync(userFeedbackToAnalyze);
                _logger.LogError("LLM analysis failed for feedback {FeedbackId}: {Error}", userFeedbackToAnalyze.Id.Value, llmAnalysis.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during LLM analysis for feedback {FeedbackId}", userFeedbackToAnalyze.Id.Value);
            // Catch any unexpected exceptions during async processing
            var failureDetails = new AnalysisFailureDetails(
                FailureReason.Unknown,
                ex.Message,
                new Timestamp(_timeProvider),
                userFeedbackToAnalyze.RetryCount + 1 // Increment retry count for this failure
            );

            userFeedbackToAnalyze.MarkAsFailed(failureDetails);
            _ = await _userFeedbackRepository.UpdateAsync(userFeedbackToAnalyze);
        }
    }

    private async Task<FeedbackAnalysisResult> BuildAnalysisResult(LlmAnalysisResult value)
    {
        ISet<FeatureCategory> featureCategories = await GetOrCreateFeatureCategories(value.FeatureCategoryNames);
        return new FeedbackAnalysisResult(
            value.Title,
            value.Sentiment,
            value.FeedbackCategories,
            featureCategories,
            new Timestamp(_timeProvider)
        );
    }

    private async Task<ISet<FeatureCategory>> GetOrCreateFeatureCategories(ISet<string> featureCategoryNames)
    {
        ISet<FeatureCategory> existing = await _featureCategoryWriteRepository.GetByNamesAsync(featureCategoryNames);
        ISet<string> namesOfMissingCategories = featureCategoryNames
            .Except(existing.Select(fc => fc.Name.Value))
            .ToHashSet();
        HashSet<FeatureCategory> addedCategories = [];
        foreach (string name in namesOfMissingCategories)
        {
            Result<FeatureCategory> result =
                await _featureCategoryWriteRepository.AddAsync(new FeatureCategory(new FeatureCategoryName(name), _timeProvider));
            if (result.IsSuccess)
            {
                addedCategories.Add(result.Value);
            }
            else
            {
                _logger.LogWarning("Failed to add new feature category '{FeatureCategoryName}': {Error}", name, result.Error);
            }
        }
        return existing.Union(addedCategories).ToHashSet();
    }

    private FailureReason MapLLMErrorToFailureReason(string errorMessage)
    {
        if (errorMessage.Contains("LLM service returned", StringComparison.OrdinalIgnoreCase) || errorMessage.Contains("network error", StringComparison.OrdinalIgnoreCase))
        {
            return FailureReason.LlmError;
        }
        if (errorMessage.Contains("LLM does not succeed", StringComparison.OrdinalIgnoreCase) || errorMessage.Contains("not able to analyze", StringComparison.OrdinalIgnoreCase))
        {
            return FailureReason.LlmUnableToProcess;
        }
        return FailureReason.Unknown;
    }
}
