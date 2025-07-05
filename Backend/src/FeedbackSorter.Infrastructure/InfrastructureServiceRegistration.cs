using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackReadRepository;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Application.Notifications;
using FeedbackSorter.Infrastructure.LLM;
using FeedbackSorter.Infrastructure.Notifications;
using FeedbackSorter.Infrastructure.Persistence;
using FeedbackSorter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FeedbackSorter.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddDbContext<FeedbackSorterDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"),
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            );

        services.AddScoped<IUserFeedbackRepository, EfUserFeedbackRepository>();
        services.AddScoped<IUserFeedbackReadRepository, EfUserFeedbackReadRepository>();
        services.AddScoped<IFeatureCategoryRepository, EfFeatureCategoryRepository>();
        services.AddScoped<IFeatureCategoryReadRepository, EfFeatureCategoryReadRepository>();
        services.AddSingleton<ILlmFeedbackAnalyzer, SemanticKernelLlmAnalyzer>();
        services.AddSingleton<ITimeProvider>(new SystemTimeProvider());
        services.AddScoped<IFeedbackNotificationService, SignalRFeedbackNotificationService>();
        return services;
    }
}
