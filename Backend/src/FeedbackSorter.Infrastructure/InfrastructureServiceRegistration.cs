using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Application.UserFeedback;
using FeedbackSorter.Infrastructure.FeatureCategories;
using FeedbackSorter.Infrastructure.LLM;
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
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"))); // Get connection string from config

        services.AddScoped<IUserFeedbackRepository, EfUserFeedbackRepository>();
        services.AddScoped<IUserFeedbackReadRepository, EfUserFeedbackRepository>();
        services.AddScoped<IFeatureCategoryRepository, EfFeatureCategoryRepository>();
        services.AddScoped<IFeatureCategoryReadRepository, EfFeatureCategoryRepository>();


        services.AddSingleton<ILlmFeedbackAnalyzer>(new FakeLLMFeedbackAnalyzer());
        services.AddSingleton<ITimeProvider>(new SystemTimeProvider());


        return services;
    }
}
