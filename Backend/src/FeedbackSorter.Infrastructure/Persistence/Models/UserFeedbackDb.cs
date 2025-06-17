// Path: Backend/src/FeedbackSorter.Infrastructure/Persistence/Models/UserFeedbackDb.cs
namespace FeedbackSorter.Infrastructure.Persistence.Models;

public class UserFeedbackDb
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string AnalysisStatus { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public string? AnalysisResultTitle { get; set; }
    public string? AnalysisResultSentiment { get; set; }
    public DateTime? AnalysisResultAnalyzedAt { get; set; }
    public ICollection<FeatureCategoryDb> AnalysisResultFeatureCategories { get; set; } = new List<FeatureCategoryDb>();
    public string? LastFailureDetailsReason { get; set; }
    public string? LastFailureDetailsMessage { get; set; }
    public DateTime? LastFailureDetailsOccurredAt { get; set; }
    public int? LastFailureDetailsAttemptNumber { get; set; }

    public ICollection<UserFeedbackSelectedCategoryDb> SelectedFeedbackCategories { get; set; } = new List<UserFeedbackSelectedCategoryDb>();

    public void OverwriteDataFrom(UserFeedbackDb source)
    {
        // Do not overwrite Id
        Text = source.Text;
        SubmittedAt = source.SubmittedAt;
        AnalysisStatus = source.AnalysisStatus;
        RetryCount = source.RetryCount;

        // Handle AnalysisResult properties
        AnalysisResultTitle = source.AnalysisResultTitle;
        AnalysisResultSentiment = source.AnalysisResultSentiment;
        AnalysisResultAnalyzedAt = source.AnalysisResultAnalyzedAt;

        // Clear and re-add FeatureCategories for AnalysisResult
        AnalysisResultFeatureCategories.Clear();
        foreach (FeatureCategoryDb fc in source.AnalysisResultFeatureCategories)
        {
            AnalysisResultFeatureCategories.Add(fc);
        }

        // Handle LastFailureDetails properties
        LastFailureDetailsReason = source.LastFailureDetailsReason;
        LastFailureDetailsMessage = source.LastFailureDetailsMessage;
        LastFailureDetailsOccurredAt = source.LastFailureDetailsOccurredAt;
        LastFailureDetailsAttemptNumber = source.LastFailureDetailsAttemptNumber;

        SelectedFeedbackCategories.Clear();
        foreach (UserFeedbackSelectedCategoryDb sfc in source.SelectedFeedbackCategories)
        {
            SelectedFeedbackCategories.Add(new UserFeedbackSelectedCategoryDb
            {
                UserFeedbackDbId = Id, // Ensure the correct UserFeedbackDbId is set
                FeedbackCategoryValue = sfc.FeedbackCategoryValue
            });
        }
    }
}
