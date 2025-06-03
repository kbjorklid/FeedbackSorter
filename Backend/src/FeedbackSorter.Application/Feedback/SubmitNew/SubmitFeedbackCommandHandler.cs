using FeedbackSorter.Application.Feedback.AnalyzeFeedback;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.SubmitNew;

public class SubmitFeedbackCommandHandler
{
    private readonly IUserFeedbackRepository _userFeedbackRepository;
    private readonly ILogger<SubmitFeedbackCommandHandler> _logger;
    private readonly IAnalyzeFeedbackCommandHandler _analyzeFeedbackCommandHandler;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public SubmitFeedbackCommandHandler(
        IUserFeedbackRepository userFeedbackRepository,
        ILogger<SubmitFeedbackCommandHandler> logger,
        IAnalyzeFeedbackCommandHandler analyzeFeedbackCommandHandler,
        IServiceScopeFactory serviceScopeFactory)
    {
        _userFeedbackRepository = userFeedbackRepository;
        _logger = logger;
        _analyzeFeedbackCommandHandler = analyzeFeedbackCommandHandler;
        _serviceScopeFactory = serviceScopeFactory;
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
        feedbackId = addResult.Value.Id;

        _logger.LogDebug("Successfully added initial UserFeedback with ID: {FeedbackId}. Starting analysis in background.", feedbackId.Value);
        _ = Task.Run(async () =>
        {
            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                IAnalyzeFeedbackCommandHandler scopedAnalyzeFeedbackCommandHandler = scope.ServiceProvider.GetRequiredService<IAnalyzeFeedbackCommandHandler>();
                AnalyzeFeedbackCommand analyzeCommand = new(feedbackId);
                await scopedAnalyzeFeedbackCommandHandler.Handle(analyzeCommand, CancellationToken.None);
                _logger.LogDebug("Background analysis task for FeedbackId {FeedbackId} completed.", feedbackId.Value);
            }
        });

        return Result<FeedbackId>.Success(feedbackId);
    }
}
