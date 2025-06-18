using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.Feedback;
using FeedbackSorter.Application.Feedback.AnalyzeFeedback;
using FeedbackSorter.Application.Feedback.GetAnalyzedFeedbacks;
using FeedbackSorter.Application.Feedback.MarkAnalysisFailed;
using FeedbackSorter.Application.Feedback.MarkAnalyzed;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FeedbackSorter.SystemTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public ILlmFeedbackAnalyzer LLMFeedbackAnalyzerMock { get; private set; } = null!;
    public ITimeProvider TimeProviderMock { get; private set; } = null!;
    public ILogger<AnalyzeFeedbackCommandHandler> AnalyzeFeedbackCommandHandlerLoggerMock { get; private set; } = null!;
    public ILogger<MarkFeedbackAnalyzedCommandHandler> MarkFeedbackAnalyzedCommandHandlerLoggerMock { get; private set; } = null!;
    public ILogger<MarkFeedbackAnalysisFailedCommandHandler> MarkFeedbackAnalysisFailedCommandHandlerLoggerMock { get; private set; } = null!;
    public ILogger<GetAnalyzedFeedbacksQueryHandler> GetAnalyzedFeedbacksQueryHandlerLoggerMock { get; private set; } = null!;


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing infrastructure registrations
            var infrastructureServiceDescriptors = services.Where(
                descriptor => 
                              descriptor.ServiceType == typeof(ILlmFeedbackAnalyzer) ||
                              descriptor.ServiceType == typeof(ITimeProvider) ||
                              descriptor.ServiceType == typeof(ILogger<AnalyzeFeedbackCommandHandler>) ||
                              descriptor.ServiceType == typeof(ILogger<MarkFeedbackAnalyzedCommandHandler>) ||
                              descriptor.ServiceType == typeof(ILogger<MarkFeedbackAnalysisFailedCommandHandler>) ||
                              descriptor.ServiceType == typeof(ILogger<GetAnalyzedFeedbacksQueryHandler>))
                .ToList();

            foreach (ServiceDescriptor? descriptor in infrastructureServiceDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add mock implementations
            LLMFeedbackAnalyzerMock = Substitute.For<ILlmFeedbackAnalyzer>();
            TimeProviderMock = Substitute.For<ITimeProvider>();
            AnalyzeFeedbackCommandHandlerLoggerMock = Substitute.For<ILogger<AnalyzeFeedbackCommandHandler>>();
            MarkFeedbackAnalyzedCommandHandlerLoggerMock = Substitute.For<ILogger<MarkFeedbackAnalyzedCommandHandler>>();
            MarkFeedbackAnalysisFailedCommandHandlerLoggerMock = Substitute.For<ILogger<MarkFeedbackAnalysisFailedCommandHandler>>();
            GetAnalyzedFeedbacksQueryHandlerLoggerMock = Substitute.For<ILogger<GetAnalyzedFeedbacksQueryHandler>>();


            services.AddSingleton(LLMFeedbackAnalyzerMock);
            services.AddSingleton(TimeProviderMock);
            services.AddSingleton(AnalyzeFeedbackCommandHandlerLoggerMock);
            services.AddSingleton(MarkFeedbackAnalyzedCommandHandlerLoggerMock);
            services.AddSingleton(MarkFeedbackAnalysisFailedCommandHandlerLoggerMock);
            services.AddSingleton(GetAnalyzedFeedbacksQueryHandlerLoggerMock);
        });
    }

    public void ResetMocks()
    {
        LLMFeedbackAnalyzerMock.ClearReceivedCalls();
        TimeProviderMock.ClearReceivedCalls();
        TimeProviderMock.UtcNow.Returns(new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        AnalyzeFeedbackCommandHandlerLoggerMock.ClearReceivedCalls();
        MarkFeedbackAnalyzedCommandHandlerLoggerMock.ClearReceivedCalls();
        MarkFeedbackAnalysisFailedCommandHandlerLoggerMock.ClearReceivedCalls();
        GetAnalyzedFeedbacksQueryHandlerLoggerMock.ClearReceivedCalls();
    }
}
