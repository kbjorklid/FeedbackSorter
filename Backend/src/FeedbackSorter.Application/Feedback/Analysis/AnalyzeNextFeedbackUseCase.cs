using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.Analysis;

public class AnalyzeNextFeedbackUseCase(
    GetNextFeedbackForAnalysisUseCase getNextFeedbackForAnalysisUseCase,
    AnalyzeFeedbackUseCase analyzeFeedbackUseCase)
{

    public async Task<bool> Execute(CancellationToken stoppingToken)
    {
        UserFeedback? userFeedback = await getNextFeedbackForAnalysisUseCase.Get(stoppingToken);
        if (userFeedback == null) return false;
        await analyzeFeedbackUseCase.Execute(userFeedback.Id, stoppingToken);
        return true;
    } 
}
