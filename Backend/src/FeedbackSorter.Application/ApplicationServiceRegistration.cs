using FeedbackSorter.Application.Feedback.GetAnalyzedFeedbacks;
using FeedbackSorter.Application.Feedback.MarkAnalyzed;
using FeedbackSorter.Application.Feedback.SubmitNew;
using Microsoft.Extensions.DependencyInjection;

namespace FeedbackSorter.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetAnalyzedFeedbacksQueryHandler>();
        services.AddScoped<SubmitFeedbackCommandHandler>();
        services.AddScoped<MarkFeedbackAnalyzedCommandHandler>();


        return services;
    }
}
