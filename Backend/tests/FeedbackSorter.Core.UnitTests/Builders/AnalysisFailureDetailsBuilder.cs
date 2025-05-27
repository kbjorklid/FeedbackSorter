using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.UnitTests.Builders;

public class AnalysisFailureDetailsBuilder
{
    private FailureReason _reason = FailureReason.Unknown;
    private string? _message = "Default failure message.";
    private Timestamp _occurredAt = new Timestamp(DateTime.UtcNow);
    private int _attemptNumber = 1;

    public AnalysisFailureDetailsBuilder WithReason(FailureReason reason)
    {
        _reason = reason;
        return this;
    }

    public AnalysisFailureDetailsBuilder WithMessage(string? message)
    {
        _message = message;
        return this;
    }

    public AnalysisFailureDetailsBuilder WithOccurredAt(Timestamp occurredAt)
    {
        _occurredAt = occurredAt;
        return this;
    }

    public AnalysisFailureDetailsBuilder WithAttemptNumber(int attemptNumber)
    {
        _attemptNumber = attemptNumber;
        return this;
    }

    public AnalysisFailureDetails Build()
    {
        return new AnalysisFailureDetails(_reason, _message, _occurredAt, _attemptNumber);
    }
}
