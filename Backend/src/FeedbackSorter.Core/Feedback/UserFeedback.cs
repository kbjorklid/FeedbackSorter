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
    public int RetryCount { get; private set; }
    public FeedbackAnalysisResult? AnalysisResult { get; private set; }
    public AnalysisFailureDetails? LastFailureDetails { get; private set; }

    public UserFeedback(FeedbackId id, FeedbackText text) : base(id)
    {
        Text = text;
        SubmittedAt = new Timestamp(DateTime.UtcNow);
        AnalysisStatus = AnalysisStatus.WaitingForAnalysis;
        RetryCount = 0;
    }

    public UserFeedback(
         FeedbackId id,
         FeedbackText text,
         Timestamp submittedAt,
         AnalysisStatus analysisStatus,
         int retryCount,
         FeedbackAnalysisResult? analysisResult,
         AnalysisFailureDetails? lastFailureDetails) : base(id)
    {

        Text = text;
        SubmittedAt = submittedAt;
        AnalysisStatus = analysisStatus;
        RetryCount = retryCount;
        AnalysisResult = analysisResult;
        LastFailureDetails = lastFailureDetails;

        EnsureInvariantsUpheld();
    }

    private void EnsureInvariantsUpheld()
    {
        if (RetryCount < 0)
        {
            throw new InvalidOperationException("RetryCount cannot be negative.");
        }

        if (AnalysisStatus == AnalysisStatus.Analyzed && AnalysisResult == null)
        {
            throw new InvalidOperationException("AnalysisResult cannot be null when AnalysisStatus is Analyzed.");
        }

        if (AnalysisStatus != AnalysisStatus.Analyzed && AnalysisResult != null)
        {
            throw new InvalidOperationException("AnalysisResult must be null when AnalysisStatus is not Analyzed.");
        }

        if (AnalysisStatus == AnalysisStatus.AnalysisFailed && LastFailureDetails == null)
        {
            throw new InvalidOperationException("LastFailureDetails cannot be null when AnalysisStatus is AnalysisFailed.");
        }

        if (AnalysisStatus != AnalysisStatus.AnalysisFailed && LastFailureDetails != null)
        {
            throw new InvalidOperationException("LastFailureDetails must be null when AnalysisStatus is not AnalysisFailed.");
        }

        if ((AnalysisStatus == AnalysisStatus.Processing || AnalysisStatus == AnalysisStatus.WaitingForAnalysis) && (AnalysisResult != null || LastFailureDetails != null))
        {
            throw new InvalidOperationException("AnalysisResult and LastFailureDetails cannot be set when AnalysisStatus is Processing or WaitingForAnalysis.");
        }
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
        RetryCount++;
    }
}
