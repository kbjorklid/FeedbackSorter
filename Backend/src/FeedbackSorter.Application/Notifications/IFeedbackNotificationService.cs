using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Notifications;

public interface IFeedbackNotificationService
{
    Task NotifyFeedbackAnalyzed(FeedbackId feedbackId, CancellationToken cancellationToken = default);
    Task NotifyFeedbackAnalysisFailed(FeedbackId feedbackId, CancellationToken cancellationToken = default);
}