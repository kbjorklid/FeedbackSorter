using Microsoft.Extensions.DependencyInjection;

namespace FeedbackSorter.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application services here
        return services;
    }
}
