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
                await AnalyzeFeedbacks(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred during processing.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task AnalyzeFeedbacks(CancellationToken stoppingToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        AnalyzeNextFeedbackUseCase analyzeUseCase =
            scope.ServiceProvider.GetRequiredService<AnalyzeNextFeedbackUseCase>();

        while (!stoppingToken.IsCancellationRequested)
        {
            bool analyzed = await analyzeUseCase.Execute(stoppingToken);
            if (!analyzed) break;
        }
    }
}
