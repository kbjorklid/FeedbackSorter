using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.Feedback;

/// <summary>
/// Represents user feedback in the system.
/// </summary>
public class UserFeedback : Entity<FeedbackId>
{
    public FeedbackText Text { get; }
    public Timestamp SubmittedAt { get; }
    public AnalysisStatus AnalysisStatus { get; private set; }
    public RetryCount RetryCount { get; private set; }
    public FeedbackAnalysisResult? AnalysisResult { get; private set; }
    public AnalysisFailureDetails? LastFailureDetails { get; private set; }

    public UserFeedback(FeedbackId id, FeedbackText text) : base(id)
    {
        Text = text;
        SubmittedAt = new Timestamp(DateTime.UtcNow);
        AnalysisStatus = AnalysisStatus.WaitingForAnalysis;
        RetryCount = new RetryCount(0);
    }

    public void StartProcessing()
    {
        AnalysisStatus = AnalysisStatus.Processing;
        LastFailureDetails = null;
        AnalysisResult = null;
    }

    public void MarkAsAnalyzed(FeedbackAnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        AnalysisStatus = AnalysisStatus.Analyzed;
        LastFailureDetails = null;
        AnalysisResult = result;
    }

    public void MarkAsFailed(AnalysisFailureDetails failureDetails)
    {
        AnalysisStatus = AnalysisStatus.AnalysisFailed;
        LastFailureDetails = failureDetails;
        AnalysisResult = null;
    }

    public void ResetForRetry()
    {
        AnalysisStatus = AnalysisStatus.WaitingForAnalysis;
        LastFailureDetails = null;
        AnalysisResult = null;
        RetryCount = RetryCount.Increment();
    }
}
