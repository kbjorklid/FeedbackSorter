using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.Feedback;
using FeedbackSorter.Application.Feedback.Analysis;
using FeedbackSorter.Application.Feedback.Query;
using FeedbackSorter.Application.Feedback.Submit;
using Microsoft.Extensions.DependencyInjection;

namespace FeedbackSorter.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<QueryAnalyzedFeedbacksUseCase>();
        services.AddScoped<SubmitFeedbackUseCase>();
        services.AddScoped<MarkFeedbackAnalyzedUseCase>();
        services.AddScoped<MarkFeedbackAnalysisFailedUseCase>();
        services.AddScoped<CreateOrGetFeatureCategoriesUseCase>();
        services.AddScoped<GetNextFeedbackForAnalysisUseCase>();
        services.AddScoped<AnalyzeFeedbackUseCase>();
        services.AddScoped<AnalyzeNextFeedbackUseCase>();
        services.AddScoped<FlagFeedbackForReanalysisUseCase>();

        services.AddHostedService<BackgroundAnalysisService>();

        return services;
    }
}
