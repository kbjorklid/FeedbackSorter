using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackRepository;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback.Analysis;

public class FlagFeedbackForReanalysisUseCase(IUserFeedbackRepository userFeedbackRepository)
{

    public async Task<bool> Execute(FeedbackId feedbackId)
    {
        Result<UserFeedback> feedback = await userFeedbackRepository.GetByIdAsync(feedbackId);
        if (!feedback.IsSuccess)
            return false;
        UserFeedback value = feedback.Value;
        if (value.AnalysisStatus == AnalysisStatus.Processing)
            return true; // already being analysed
        
        value.ResetForRetry();

        Result<UserFeedback> updateAsync = await userFeedbackRepository.UpdateAsync(value);
        return updateAsync.IsSuccess;
    }
}
