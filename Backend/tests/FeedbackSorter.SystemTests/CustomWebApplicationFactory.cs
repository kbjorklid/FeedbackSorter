using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.Feedback;
using FeedbackSorter.Application.Feedback.AnalyzeFeedback;
using FeedbackSorter.Application.Feedback.MarkAnalysisFailed;
using FeedbackSorter.Application.Feedback.MarkAnalyzed;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FeedbackSorter.SystemTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public IUserFeedbackReadRepository UserFeedbackReadRepositoryMock { get; private set; } = null!;
    public IUserFeedbackRepository UserFeedbackRepositoryMock { get; private set; } = null!;
    public IFeatureCategoryRepository FeatureCategoryRepositoryMock { get; private set; } = null!;
    public IFeatureCategoryReadRepository FeatureCategoryReadRepositoryMock { get; private set; } = null!;
    public ILlmFeedbackAnalyzer LLMFeedbackAnalyzerMock { get; private set; } = null!;
    public ITimeProvider TimeProviderMock { get; private set; } = null!;
    public IAnalyzeFeedbackCommandHandler AnalyzeFeedbackCommandHandlerMock { get; private set; } = null!;


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing infrastructure registrations
            var infrastructureServiceDescriptors = services.Where(
                descriptor => descriptor.ServiceType == typeof(IUserFeedbackReadRepository) ||
                              descriptor.ServiceType == typeof(IUserFeedbackRepository) ||
                              descriptor.ServiceType == typeof(IFeatureCategoryRepository) ||
                              descriptor.ServiceType == typeof(IFeatureCategoryReadRepository) ||
                              descriptor.ServiceType == typeof(ILlmFeedbackAnalyzer) ||
                              descriptor.ServiceType == typeof(ITimeProvider) ||
                              descriptor.ServiceType == typeof(IAnalyzeFeedbackCommandHandler) || // Changed to interface
                              descriptor.ServiceType == typeof(AnalyzeFeedbackCommandHandler) || // Remove concrete registration
                              descriptor.ServiceType == typeof(MarkFeedbackAnalyzedCommandHandler) ||
                              descriptor.ServiceType == typeof(MarkFeedbackAnalysisFailedCommandHandler))
                .ToList();

            foreach (ServiceDescriptor? descriptor in infrastructureServiceDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add mock implementations
            UserFeedbackReadRepositoryMock = Substitute.For<IUserFeedbackReadRepository>();
            UserFeedbackRepositoryMock = Substitute.For<IUserFeedbackRepository>();
            FeatureCategoryRepositoryMock = Substitute.For<IFeatureCategoryRepository>();
            FeatureCategoryReadRepositoryMock = Substitute.For<IFeatureCategoryReadRepository>();
            LLMFeedbackAnalyzerMock = Substitute.For<ILlmFeedbackAnalyzer>();
            TimeProviderMock = Substitute.For<ITimeProvider>();

            // Mock concrete command handlers that AnalyzeFeedbackCommandHandler depends on
            MarkFeedbackAnalyzedCommandHandler markFeedbackAnalyzedCommandHandlerMock = Substitute.For<MarkFeedbackAnalyzedCommandHandler>(UserFeedbackRepositoryMock, FeatureCategoryRepositoryMock);
            MarkFeedbackAnalysisFailedCommandHandler markFeedbackAnalysisFailedCommandHandlerMock = Substitute.For<MarkFeedbackAnalysisFailedCommandHandler>(UserFeedbackRepositoryMock, TimeProviderMock);

            // Mock the AnalyzeFeedbackCommandHandler interface
            AnalyzeFeedbackCommandHandlerMock = Substitute.For<IAnalyzeFeedbackCommandHandler>();

            services.AddSingleton(UserFeedbackReadRepositoryMock);
            services.AddSingleton(UserFeedbackRepositoryMock);
            services.AddSingleton(FeatureCategoryRepositoryMock);
            services.AddSingleton(FeatureCategoryReadRepositoryMock);
            services.AddSingleton(LLMFeedbackAnalyzerMock);
            services.AddSingleton(TimeProviderMock);
            services.AddSingleton(markFeedbackAnalyzedCommandHandlerMock); // Register mocked concrete
            services.AddSingleton(markFeedbackAnalysisFailedCommandHandlerMock); // Register mocked concrete
            services.AddSingleton(AnalyzeFeedbackCommandHandlerMock); // Register mocked interface
        });
    }

    public void ResetMocks()
    {
        UserFeedbackReadRepositoryMock.ClearReceivedCalls();
        UserFeedbackRepositoryMock.ClearReceivedCalls();
        FeatureCategoryRepositoryMock.ClearReceivedCalls();
        FeatureCategoryReadRepositoryMock.ClearReceivedCalls();
        LLMFeedbackAnalyzerMock.ClearReceivedCalls();
        TimeProviderMock.ClearReceivedCalls();
        AnalyzeFeedbackCommandHandlerMock.ClearReceivedCalls();
    }
}
