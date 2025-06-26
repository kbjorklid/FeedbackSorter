using FeedbackSorter.Application.Feedback.Commands.AnalyzeFeedback;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.Commands.SubmitNew;

public class SubmitFeedbackCommandHandler(
    IUserFeedbackRepository userFeedbackRepository,
    ILogger<SubmitFeedbackCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{

    public async Task<Result<FeedbackId>> HandleAsync(SubmitFeedbackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var feedbackId = FeedbackId.New();

        var initialUserFeedback = new UserFeedback(
            feedbackId,
            new FeedbackText(command.Text)
        );
        Result<UserFeedback> addResult = await userFeedbackRepository.AddAsync(initialUserFeedback);

        if (addResult.IsFailure)
        {
            logger.LogError("Failed to add initial UserFeedback to repository: {Error}", addResult.Error);
            return Result<FeedbackId>.Failure($"Failed to add initial UserFeedback to repository: {addResult.Error}");
        }
        feedbackId = addResult.Value.Id;

        logger.LogDebug("Successfully added initial UserFeedback with ID: {FeedbackId}. Starting analysis in background.", feedbackId.Value);
        _ = Task.Run(async () =>
        {
            using (IServiceScope scope = serviceScopeFactory.CreateScope())
            {
                IAnalyzeFeedbackCommandHandler scopedAnalyzeFeedbackCommandHandler = scope.ServiceProvider.GetRequiredService<IAnalyzeFeedbackCommandHandler>();
                AnalyzeFeedbackCommand analyzeCommand = new(feedbackId);
                await scopedAnalyzeFeedbackCommandHandler.Handle(analyzeCommand, CancellationToken.None);
                logger.LogDebug("Background analysis task for FeedbackId {FeedbackId} completed.", feedbackId.Value);
            }
        });

        return Result<FeedbackId>.Success(feedbackId);
    }
}
