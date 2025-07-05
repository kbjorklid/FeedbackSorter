using FeedbackSorter.Application.Notifications;
using FeedbackSorter.Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FeedbackSorter.Presentation.Notifications;

public class FeedbackHubContextWrapper : IFeedbackHubContext
{
    private readonly IHubContext<FeedbackHub> _hubContext;

    public FeedbackHubContextWrapper(IHubContext<FeedbackHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToGroupAsync(string groupName, string method, object? arg, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(groupName).SendAsync(method, arg, cancellationToken);
    }
}