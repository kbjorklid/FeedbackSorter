using FeedbackSorter.Application.Feedback.AnalyzeFeedback;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback.SubmitNew;

public class SubmitFeedbackCommandHandler
{
    private readonly IUserFeedbackRepository _userFeedbackRepository;
    private readonly ILogger<SubmitFeedbackCommandHandler> _logger;
    private readonly IAnalyzeFeedbackCommandHandler _analyzeFeedbackCommandHandler;


    public SubmitFeedbackCommandHandler(
        IUserFeedbackRepository userFeedbackRepository,
        ILogger<SubmitFeedbackCommandHandler> logger,
        IAnalyzeFeedbackCommandHandler analyzeFeedbackCommandHandler)
    {
        _userFeedbackRepository = userFeedbackRepository;
        _logger = logger;
        _analyzeFeedbackCommandHandler = analyzeFeedbackCommandHandler;
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

        _ = Task.Run(async () =>
        {
            AnalyzeFeedbackCommand analyzeCommand = new(addResult.Value.Id);
            await _analyzeFeedbackCommandHandler.Handle(analyzeCommand, CancellationToken.None);
        });

        return Result<FeedbackId>.Success(feedbackId);
    }
}
