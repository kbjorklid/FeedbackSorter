using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
namespace FeedbackSorter.Application.UserFeedback.SubmitNew;

public class SubmitFeedbackCommandHandler
{
    private readonly IUserFeedbackRepository _userFeedbackRepository;
    private readonly ILLMFeedbackAnalyzer _llmFeedbackAnalyzer;
    private readonly ITimeProvider _timeProvider;
    private readonly IFeatureCategoryReadRepository _featureCategoryReadRepository;

    public SubmitFeedbackCommandHandler(
        IUserFeedbackRepository userFeedbackRepository,
        ILLMFeedbackAnalyzer llmFeedbackAnalyzer,
        ITimeProvider timeProvider,
        IFeatureCategoryReadRepository featureCategoryReadRepository)
    {
        _userFeedbackRepository = userFeedbackRepository;
        _llmFeedbackAnalyzer = llmFeedbackAnalyzer;
        _timeProvider = timeProvider;
        _featureCategoryReadRepository = featureCategoryReadRepository;
    }

    public async Task<Result<FeedbackId>> HandleAsync(SubmitFeedbackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 1. Generate ID and Timestamp
        var feedbackId = new FeedbackId(Guid.NewGuid());
        var submittedAt = new Timestamp(_timeProvider);

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
                IEnumerable<FeatureCategories.Queries.FeatureCategoryReadModel> existingFeatureCategoriesReadModels = await _featureCategoryReadRepository.GetAllAsync();
                var existingFeatureCategories = existingFeatureCategoriesReadModels
                    .Select(fc => new FeatureCategory(fc.Id, fc.Name, _timeProvider))
                    .ToList();

                // Define sentiment and feedback category choices
                Sentiment[] sentimentChoices = Enum.GetValues<Sentiment>();
                FeedbackCategoryType[] feedbackCategoryChoices = Enum.GetValues<FeedbackCategoryType>();

                Result<FeedbackAnalysisResult> analysisResult = await _llmFeedbackAnalyzer.AnalyzeFeedback(
                    userFeedbackToAnalyze.Text,
                    existingFeatureCategories,
                    sentimentChoices,
                    feedbackCategoryChoices
                );

                if (analysisResult.IsSuccess)
                {
                    userFeedbackToAnalyze.MarkAsAnalyzed(analysisResult.Value);
                    _ = await _userFeedbackRepository.UpdateAsync(userFeedbackToAnalyze);
                }
                else
                {
                    // LLM analysis failed
                    FailureReason failureReason = MapLLMErrorToFailureReason(analysisResult.Error);
                    var failureDetails = new AnalysisFailureDetails(
                        failureReason,
                        analysisResult.Error,
                        new Timestamp(_timeProvider),
                        userFeedbackToAnalyze.RetryCount + 1 // Increment retry count for this failure
                    );

                    userFeedbackToAnalyze.MarkAsFailed(failureDetails);
                    _ = await _userFeedbackRepository.UpdateAsync(userFeedbackToAnalyze);
                }
            }
            catch (Exception ex)
            {
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
