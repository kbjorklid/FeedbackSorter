using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
namespace FeedbackSorter.Application.UserFeedback.SubmitNew;

public class SubmitFeedbackCommandHandler
{
    private readonly IUserFeedbackRepository _userFeedbackRepository;
    private readonly ILlmFeedbackAnalyzer _llmFeedbackAnalyzer;
    private readonly ITimeProvider _timeProvider;
    private readonly IFeatureCategoryReadRepository _featureCategoryReadRepository;
    private readonly IFeatureCategoryRepository _featureCategoryWriteRepository;


    public SubmitFeedbackCommandHandler(
        IUserFeedbackRepository userFeedbackRepository,
        ILlmFeedbackAnalyzer llmFeedbackAnalyzer,
        ITimeProvider timeProvider,
        IFeatureCategoryReadRepository featureCategoryReadRepository,
        IFeatureCategoryRepository featureCategoryWriteRepository)
    {
        _userFeedbackRepository = userFeedbackRepository;
        _llmFeedbackAnalyzer = llmFeedbackAnalyzer;
        _timeProvider = timeProvider;
        _featureCategoryReadRepository = featureCategoryReadRepository;
        _featureCategoryWriteRepository = featureCategoryWriteRepository;
    }

    public async Task<Result<FeedbackId>> HandleAsync(SubmitFeedbackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 1. Generate ID
        var feedbackId = FeedbackId.New();

        // 2. Create initial UserFeedback with WaitingForAnalysis status
        var initialUserFeedback = new FeedbackSorter.Core.Feedback.UserFeedback(
            feedbackId,
            new FeedbackText(command.Text) // Assuming validation handled by DTO/Controller
        );

        // 3. Store initial UserFeedback
        Result<Core.Feedback.UserFeedback> addResult = await _userFeedbackRepository.AddAsync(initialUserFeedback);
        if (addResult.IsFailure)
        {
            return Result<FeedbackId>.Failure($"Failed to add initial UserFeedback to repository: {addResult.Error}");
        }

        // 5. Asynchronously analyze and update feedback
        _ = Task.Run(async () =>
        {
            Core.Feedback.UserFeedback userFeedbackToAnalyze = addResult.Value; // Use the one returned from AddAsync

            try
            {
                // Get existing feature categories
                IEnumerable<FeatureCategoryReadModel> existingFeatureCategoriesReadModels = await _featureCategoryReadRepository.GetAllAsync();
                var existingFeatureCategories = existingFeatureCategoriesReadModels
                    .Select(fc => new FeatureCategoryReadModel(fc.Id, fc.Name))
                    .ToList();

                // Define sentiment and feedback category choices
                Sentiment[] sentimentChoices = Enum.GetValues<Sentiment>();
                FeedbackCategoryType[] feedbackCategoryChoices = Enum.GetValues<FeedbackCategoryType>();

                Result<LlmAnalysisResult> llmAnalysis = await _llmFeedbackAnalyzer.AnalyzeFeedback(
                    userFeedbackToAnalyze.Text,
                    existingFeatureCategories,
                    sentimentChoices,
                    feedbackCategoryChoices
                );


                if (llmAnalysis.IsSuccess)
                {
                    Console.WriteLine("LLM analysis succeeded");
                    FeedbackAnalysisResult analysisResult = await BuildAnalysisResult(llmAnalysis.Value);
                    userFeedbackToAnalyze.MarkAsAnalyzed(analysisResult);
                    Console.WriteLine("result---" + userFeedbackToAnalyze.AnalysisResult);
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ex " + ex);
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
        });

        return Result<FeedbackId>.Success(feedbackId);
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
