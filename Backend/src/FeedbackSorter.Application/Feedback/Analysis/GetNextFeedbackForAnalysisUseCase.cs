using FeedbackSorter.Application.Feedback.Repositories;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Core;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.Analysis;

public class GetNextFeedbackForAnalysisUseCase(IUserFeedbackRepository userFeedbackRepository)
{
    public async Task<UserFeedback?> Get(CancellationToken stoppingToken = default)
    {
        IList<UserFeedback> results = await userFeedbackRepository.QueryAsync(new UserFeedbackQuery
        {
            AnalysisStatuses = new HashSet<AnalysisStatus> { AnalysisStatus.WaitingForAnalysis },
            MaxResults = 1,
            SortBy = UserFeedbackSortBy.SubmittedAt,
            Order = SortOrder.Asc
        }, stoppingToken);
        return results.FirstOrDefault();
    }
}
