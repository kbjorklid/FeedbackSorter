using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.Feedback;
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
                              descriptor.ServiceType == typeof(ITimeProvider))
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

            services.AddSingleton(UserFeedbackReadRepositoryMock);
            services.AddSingleton(UserFeedbackRepositoryMock);
            services.AddSingleton(FeatureCategoryRepositoryMock);
            services.AddSingleton(FeatureCategoryReadRepositoryMock);
            services.AddSingleton(LLMFeedbackAnalyzerMock);
            services.AddSingleton(TimeProviderMock);
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
    }
}
