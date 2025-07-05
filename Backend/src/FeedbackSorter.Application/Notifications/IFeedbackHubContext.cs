namespace FeedbackSorter.Application.Notifications;

public interface IFeedbackHubContext
{
    Task SendToGroupAsync(string groupName, string method, object? arg, CancellationToken cancellationToken = default);
}