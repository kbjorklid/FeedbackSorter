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

        var initialUserFeedback = new UserFeedback(
            feedbackId,
            new FeedbackText(command.Text)
        );
        Result<UserFeedback> addResult = await _userFeedbackRepository.AddAsync(initialUserFeedback);
        if (addResult.IsFailure)
        {
            _logger.LogError("Failed to add initial UserFeedback to repository: {Error}", addResult.Error);
            return Result<FeedbackId>.Failure($"Failed to add initial UserFeedback to repository: {addResult.Error}");
        }

        _ = Task.Run(async () => await AnalyzeAndSaveFeedbackAsync(addResult.Value));

        return Result<FeedbackId>.Success(feedbackId);
    }

    private async Task AnalyzeAndSaveFeedbackAsync(UserFeedback userFeedbackToAnalyze)
    {
        try
        {
            IEnumerable<FeatureCategoryReadModel> existingFeatureCategoriesReadModels = await _featureCategoryReadRepository.GetAllAsync();

            LlmAnalysisResult llmAnalysis = await _llmFeedbackAnalyzer.AnalyzeFeedback(
                userFeedbackToAnalyze.Text,
                existingFeatureCategoriesReadModels
            );


            if (llmAnalysis.IsSuccess)
            {
                _logger.LogInformation("LLM analysis succeeded for feedback {FeedbackId}", userFeedbackToAnalyze.Id.Value);
                FeedbackAnalysisResult analysisResult = await BuildAnalysisResult(llmAnalysis);
                userFeedbackToAnalyze.MarkAsAnalyzed(analysisResult);
                _logger.LogDebug("Analysis result for feedback {FeedbackId}: {AnalysisResult}", userFeedbackToAnalyze.Id.Value, userFeedbackToAnalyze.AnalysisResult);
                _ = await _userFeedbackRepository.UpdateAsync(userFeedbackToAnalyze);
            }
            else
            {
                // LLM analysis failed
                LlmAnalysisFailure failure = llmAnalysis.Failure!;
                var failureDetails = new AnalysisFailureDetails(
                    failure.Reason,
                    failure.Error,
                    llmAnalysis.AnalyzedAt,
                    userFeedbackToAnalyze.RetryCount + 1 // Increment retry count for this failure
                );

                userFeedbackToAnalyze.MarkAsFailed(failureDetails);
                _ = await _userFeedbackRepository.UpdateAsync(userFeedbackToAnalyze);
                _logger.LogError("LLM analysis failed for feedback {FeedbackId}: {Error}", userFeedbackToAnalyze.Id.Value, llmAnalysis.Failure!.Error);
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

    private async Task<FeedbackAnalysisResult> BuildAnalysisResult(LlmAnalysisResult analysisResult)
    {
        LlmAnalysisSuccess value = analysisResult.Success!;
        ISet<FeatureCategory> featureCategories = await GetOrCreateFeatureCategories(value.FeatureCategoryNames);
        return new FeedbackAnalysisResult(
            value.Title,
            value.Sentiment,
            value.FeedbackCategories,
            featureCategories,
            analysisResult.AnalyzedAt
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
}
