namespace FeedbackSorter.Core.UserFeedback;

/// <summary>
/// Represents the analysis status of user feedback.
/// </summary>
public enum AnalysisStatus
{
    WaitingForAnalysis,
    Processing,
    Analyzed,
    AnalysisFailed
}
