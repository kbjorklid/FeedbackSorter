using FeedbackSorter.Application.Notifications;
using FeedbackSorter.Core.Feedback;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Infrastructure.Notifications;

public class SignalRFeedbackNotificationService : IFeedbackNotificationService
{
    private readonly IFeedbackHubContext _hubContext;
    private readonly ILogger<SignalRFeedbackNotificationService> _logger;

    public SignalRFeedbackNotificationService(
        IFeedbackHubContext hubContext,
        ILogger<SignalRFeedbackNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyFeedbackAnalyzed(FeedbackId feedbackId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.SendToGroupAsync("Dashboard", "FeedbackAnalyzed", feedbackId.Value, cancellationToken);
            
            _logger.LogInformation("Sent FeedbackAnalyzed notification for feedback {FeedbackId}", feedbackId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FeedbackAnalyzed notification for feedback {FeedbackId}", feedbackId.Value);
        }
    }

    public async Task NotifyFeedbackAnalysisFailed(FeedbackId feedbackId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.SendToGroupAsync("Dashboard", "FeedbackAnalysisFailed", feedbackId.Value, cancellationToken);
            
            _logger.LogInformation("Sent FeedbackAnalysisFailed notification for feedback {FeedbackId}", feedbackId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FeedbackAnalysisFailed notification for feedback {FeedbackId}", feedbackId.Value);
        }
    }
}