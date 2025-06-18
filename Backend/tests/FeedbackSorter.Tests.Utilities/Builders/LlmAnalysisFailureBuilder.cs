using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Tests.Utilities.Builders;

public class LlmAnalysisFailureBuilder
{
    private string _error = "Default error message";
    private FailureReason _reason = FailureReason.Unknown;

    public LlmAnalysisFailureBuilder WithError(string error)
    {
        _error = error;
        return this;
    }

    public LlmAnalysisFailureBuilder WithReason(FailureReason reason)
    {
        _reason = reason;
        return this;
    }

    public LlmAnalysisFailure Build()
    {
        return new LlmAnalysisFailure
        {
            Error = _error,
            Reason = _reason
        };
    }
}
