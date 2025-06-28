using FeedbackSorter.Application.Feedback.Analysis;
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
        GetNextFeedbackForAnalysisUseCase getNextFeedbackForAnalysisUseCase =
            scope.ServiceProvider.GetRequiredService<GetNextFeedbackForAnalysisUseCase>();

        UserFeedback? userFeedback = await getNextFeedbackForAnalysisUseCase.Get();
        if (userFeedback == null) return;

        var analyzeFeedbackUseCase = scope.ServiceProvider.GetRequiredService<AnalyzeFeedbackUseCase>();
        await analyzeFeedbackUseCase.Execute(userFeedback.Id, stoppingToken);
    }
}
