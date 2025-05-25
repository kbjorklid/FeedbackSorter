using FeedbackSorter.SharedKernel;
using System;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Core.UnitTests.Builders;

public class UserFeedbackBuilder
{
    private FeedbackId _id = new FeedbackIdBuilder().Build();
    private FeedbackText _text = new FeedbackTextBuilder().Build();
    private Timestamp _submittedAt = new TimestampBuilder().Build();
    private AnalysisStatus _analysisStatus = AnalysisStatus.WaitingForAnalysis;
    private RetryCount _retryCount = new RetryCountBuilder().Build();
    private FeedbackAnalysisResult? _analysisResult = null;
    private AnalysisFailureDetails? _lastFailureDetails = null;

    public UserFeedbackBuilder WithId(FeedbackId id)
    {
        _id = id;
        return this;
    }

    public UserFeedbackBuilder WithText(FeedbackText text)
    {
        _text = text;
        return this;
    }

    public UserFeedbackBuilder WithSubmittedAt(Timestamp submittedAt)
    {
        _submittedAt = submittedAt;
        return this;
    }

    public UserFeedbackBuilder WithAnalysisStatus(AnalysisStatus analysisStatus)
    {
        _analysisStatus = analysisStatus;
        return this;
    }

    public UserFeedbackBuilder WithRetryCount(RetryCount retryCount)
    {
        _retryCount = retryCount;
        return this;
    }

    public UserFeedbackBuilder WithAnalysisResult(FeedbackAnalysisResult? analysisResult)
    {
        _analysisResult = analysisResult;
        return this;
    }

    public UserFeedbackBuilder WithLastFailureDetails(AnalysisFailureDetails? lastFailureDetails)
    {
        _lastFailureDetails = lastFailureDetails;
        return this;
    }

    public UserFeedback Build()
    {
        // The UserFeedback constructor only takes id and text.
        // Other properties are set via methods or internal logic.
        // So, we'll build a base UserFeedback and then apply other states.
        var userFeedback = new UserFeedback(_id, _text);

        // Apply other states if they differ from the default constructor initialization
        if (_submittedAt.Value != userFeedback.SubmittedAt.Value)
        {
            // This is tricky as SubmittedAt is set in constructor and is readonly.
            // For testing purposes, if a specific SubmittedAt is needed, it should be passed to the constructor.
            // However, the UserFeedback constructor doesn't allow setting SubmittedAt.
            // For now, I'll assume the default SubmittedAt from the constructor is acceptable for most tests.
            // If a test *must* have a specific SubmittedAt, it would require a different approach (e.g., mocking ITimeProvider).
            // For the purpose of this builder, I will not try to override SubmittedAt after construction.
        }

        // Apply other states that can be modified after construction
        if (_analysisStatus != AnalysisStatus.WaitingForAnalysis)
        {
            if (_analysisStatus == AnalysisStatus.Processing)
            {
                userFeedback.StartProcessing();
            }
            else if (_analysisStatus == AnalysisStatus.Analyzed && _analysisResult != null)
            {
                userFeedback.MarkAsAnalyzed(_analysisResult);
            }
            else if (_analysisStatus == AnalysisStatus.AnalysisFailed && _lastFailureDetails != null)
            {
                userFeedback.MarkAsFailed(_lastFailureDetails);
            }
            // If WaitingForAnalysis, it's already the default.
        }

        // Handle retry count separately as it's incremented
        if (_retryCount.Value > 0)
        {
            for (int i = 0; i < _retryCount.Value; i++)
            {
                userFeedback.ResetForRetry();
            }
        }
        
        // Ensure analysis result and failure details are set correctly based on final status
        if (_analysisStatus == AnalysisStatus.Analyzed)
        {
            // If it's analyzed, ensure result is set and failure details are null
            if (_analysisResult == null)
            {
                // This scenario implies an invalid state for the builder, but for robustness,
                // we can provide a default analysis result if not explicitly set.
                userFeedback.MarkAsAnalyzed(new FeedbackAnalysisResultBuilder().Build());
            }
            else
            {
                userFeedback.MarkAsAnalyzed(_analysisResult);
            }
        }
        else if (_analysisStatus == AnalysisStatus.AnalysisFailed)
        {
            // If it's failed, ensure failure details are set and analysis result is null
            if (_lastFailureDetails == null)
            {
                userFeedback.MarkAsFailed(new AnalysisFailureDetailsBuilder().Build());
            }
            else
            {
                userFeedback.MarkAsFailed(_lastFailureDetails);
            }
        }
        else // WaitingForAnalysis or Processing
        {
            // Ensure these are null for these statuses
            // The MarkAsAnalyzed and MarkAsFailed methods throw ArgumentNullException if null is passed.
            // This builder should not attempt to set null if the current state is not null,
            // as the UserFeedback class itself doesn't provide a way to "unset" these properties to null directly.
            // The UserFeedback class methods (StartProcessing, MarkAsAnalyzed, MarkAsFailed, ResetForRetry)
            // handle setting these to null as part of their state transitions.
            // For the builder, we only set them if they are explicitly provided and the status matches.
            // If the status is WaitingForAnalysis or Processing, these properties should naturally be null.
            // No action needed here to explicitly null them out if they are already null by default or by previous state changes.
        }

        return userFeedback;
    }
}
