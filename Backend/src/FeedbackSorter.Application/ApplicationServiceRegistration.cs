using FeedbackSorter.Application.Feedback.Commands.AnalyzeFeedback;
using FeedbackSorter.Application.Feedback.Commands.MarkAnalysisFailed;
using FeedbackSorter.Application.Feedback.Commands.MarkAnalyzed;
using FeedbackSorter.Application.Feedback.Commands.SubmitNew;
using FeedbackSorter.Application.Feedback.Queries.GetAnalyzedFeedbacks;
using Microsoft.Extensions.DependencyInjection;

namespace FeedbackSorter.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetAnalyzedFeedbacksQueryHandler>();
        services.AddScoped<SubmitFeedbackCommandHandler>();
        services.AddScoped<MarkFeedbackAnalyzedCommandHandler>();
        services.AddScoped<MarkFeedbackAnalysisFailedCommandHandler>();
        services.AddScoped<IAnalyzeFeedbackCommandHandler, AnalyzeFeedbackCommandHandler>();

        return services;
    }
}
