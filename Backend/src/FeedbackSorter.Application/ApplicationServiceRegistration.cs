using FeedbackSorter.Application.UserFeedback.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace FeedbackSorter.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetAnalyzedFeedbacksQueryHandler>();
        return services;
    }
}
