using FeedbackSorter.Application.Feedback.Repositories;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Core;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.Queries.GetNextForAnalysis;

public class GetNextFeedbackForAnalysisCommandHandler(IUserFeedbackRepository userFeedbackRepository)
{
    public async Task<UserFeedback?> Get()
    {
        IList<UserFeedback> results = await userFeedbackRepository.QueryAsync(new UserFeedbackQuery
        {
            AnalysisStatuses = new HashSet<AnalysisStatus> { AnalysisStatus.WaitingForAnalysis },
            MaxResults = 1,
            SortBy = UserFeedbackSortBy.SubmittedAt,
            Order = SortOrder.Asc
        });
        return results.FirstOrDefault();
    }
}
