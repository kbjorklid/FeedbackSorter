using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Core.UnitTests.Builders;

namespace FeedbackSorter.Core.UnitTests;

public class UserFeedbackTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Arrange
        FeedbackId feedbackId = new FeedbackIdBuilder().Build();
        FeedbackText feedbackText = new FeedbackTextBuilder().Build();
        DateTime initialUtcTime = DateTime.UtcNow;

        // Act
        var userFeedback = new UserFeedback(feedbackId, feedbackText);

        // Assert
        Assert.Equal(feedbackId, userFeedback.Id);
        Assert.Equal(feedbackText, userFeedback.Text);
        Assert.True(userFeedback.SubmittedAt.Value.Subtract(initialUtcTime).TotalSeconds < 1); // Check within 1 second
        Assert.Equal(AnalysisStatus.WaitingForAnalysis, userFeedback.AnalysisStatus);
        Assert.Equal(0, userFeedback.RetryCount);
        Assert.Null(userFeedback.AnalysisResult);
        Assert.Null(userFeedback.LastFailureDetails);
    }

    [Fact]
    public void StartProcessing_SetsStatusToProcessingAndClearsAnalysisDetails()
    {
        // Arrange
        UserFeedback userFeedback = new UserFeedbackBuilder()
            .WithAnalysisStatus(AnalysisStatus.AnalysisFailed)
            .WithLastFailureDetails(new AnalysisFailureDetailsBuilder().Build())
            .Build();

        // Act
        userFeedback.StartProcessing();

        // Assert
        Assert.Equal(AnalysisStatus.Processing, userFeedback.AnalysisStatus);
        Assert.Null(userFeedback.LastFailureDetails);
        Assert.Null(userFeedback.AnalysisResult);
    }

    [Fact]
    public void MarkAsAnalyzed_SetsStatusToAnalyzedAndResultAndClearsFailureDetails()
    {
        // Arrange
        UserFeedback userFeedback = new UserFeedbackBuilder().Build();
        FeedbackAnalysisResult analysisResult = new FeedbackAnalysisResultBuilder().Build();

        // Act
        userFeedback.MarkAsAnalyzed(analysisResult);

        // Assert
        Assert.Equal(AnalysisStatus.Analyzed, userFeedback.AnalysisStatus);
        Assert.Equal(analysisResult, userFeedback.AnalysisResult);
        Assert.Null(userFeedback.LastFailureDetails);
    }

    [Fact]
    public void MarkAsAnalyzed_ThrowsArgumentNullException_WhenResultIsNull()
    {
        // Arrange
        var userFeedback = new UserFeedbackBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => userFeedback.MarkAsAnalyzed(null!));
    }

    [Fact]
    public void MarkAsFailed_SetsStatusToAnalysisFailedAndFailureDetailsAndClearsAnalysisResult()
    {
        // Arrange
        UserFeedback userFeedback = new UserFeedbackBuilder().Build();
        AnalysisFailureDetails failureDetails = new AnalysisFailureDetailsBuilder().Build();

        // Act
        userFeedback.MarkAsFailed(failureDetails);

        // Assert
        Assert.Equal(AnalysisStatus.AnalysisFailed, userFeedback.AnalysisStatus);
        Assert.Equal(failureDetails, userFeedback.LastFailureDetails);
        Assert.Null(userFeedback.AnalysisResult);
    }

    [Fact]
    public void ResetForRetry_ResetsStatusAndClearsDetailsAndIncrementsRetryCount()
    {
        // Arrange
        var userFeedback = new UserFeedbackBuilder()
            .WithAnalysisStatus(AnalysisStatus.AnalysisFailed)
            .WithLastFailureDetails(new AnalysisFailureDetailsBuilder().Build())
            .WithRetryCount(1)
            .Build();


        // Act
        userFeedback.ResetForRetry();

        // Assert
        Assert.Equal(AnalysisStatus.WaitingForAnalysis, userFeedback.AnalysisStatus);
        Assert.Null(userFeedback.LastFailureDetails);
        Assert.Null(userFeedback.AnalysisResult);
        Assert.Equal(2, userFeedback.RetryCount);
    }
}
