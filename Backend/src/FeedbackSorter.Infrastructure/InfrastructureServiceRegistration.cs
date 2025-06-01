using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Application.UserFeedback;
using FeedbackSorter.Infrastructure.FeatureCategories;
using FeedbackSorter.Infrastructure.Feedback;
using FeedbackSorter.Infrastructure.LLM;
using FeedbackSorter.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace FeedbackSorter.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryUserFeedbackRepository>();
        services.AddSingleton<IUserFeedbackReadRepository>(provider => provider.GetRequiredService<InMemoryUserFeedbackRepository>());
        services.AddSingleton<IUserFeedbackRepository>(provider => provider.GetRequiredService<InMemoryUserFeedbackRepository>());

        var featureCategoryRepo = new InMemoryFeatureCategoryRepository();
        services.AddSingleton<IFeatureCategoryReadRepository>(featureCategoryRepo);
        services.AddSingleton<IFeatureCategoryRepository>(featureCategoryRepo);
        services.AddSingleton<ILlmFeedbackAnalyzer>(new FakeLLMFeedbackAnalyzer());
        services.AddSingleton<ITimeProvider>(new SystemTimeProvider());


        return services;
    }
}
