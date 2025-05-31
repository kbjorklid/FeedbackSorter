using FeedbackSorter.Application.UserFeedback.GetAnalyzedFeedbacks;
using FeedbackSorter.Application.UserFeedback.SubmitNew;
using Microsoft.Extensions.DependencyInjection;

namespace FeedbackSorter.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetAnalyzedFeedbacksQueryHandler>();
        services.AddScoped<SubmitFeedbackCommandHandler>();

        return services;
    }
}
