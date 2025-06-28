using FeedbackSorter.Application.Feedback.Commands.AnalyzeFeedback;
using FeedbackSorter.Application.Feedback.Queries.GetNextForAnalysis;
using FeedbackSorter.Core.Feedback;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FeedbackSorter.Application.Feedback;

public class BackgroundAnalysisService(ILogger<BackgroundAnalysisService> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // We process one item at a time to ensure they are handled sequentially.
                await ProcessNextItemAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred during processing.");
            }
            
            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessNextItemAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var getNextFeedbackForAnalysisCommandHandler =
            scope.ServiceProvider.GetRequiredService<GetNextFeedbackForAnalysisCommandHandler>();
        
        UserFeedback? task = await getNextFeedbackForAnalysisCommandHandler.Get();
        if (task == null) return;
        
        var analyzeFeedbackCommandHandler =
            scope.ServiceProvider.GetRequiredService<AnalyzeFeedbackCommandHandler>();
        
        AnalyzeFeedbackCommand analyzeFeedbackCommand = new(task.Id);
        await analyzeFeedbackCommandHandler.Handle(analyzeFeedbackCommand, stoppingToken);
    }
}
