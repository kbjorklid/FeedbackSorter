using Microsoft.Extensions.DependencyInjection;

namespace FeedbackSorter.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register infrastructure services here
        return services;
    }
}
