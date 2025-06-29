using FeedbackSorter.Application.LLM;

namespace FeedbackSorter.Tests.Utilities.Builders;

public class LlmAnalysisResultBuilder
{
    private DateTime _analyzedAt = DateTime.UtcNow;
    private LlmAnalysisSuccess? _success = null;
    private LlmAnalysisFailure? _failure = null;

    public LlmAnalysisResultBuilder WithAnalyzedAt(DateTime analyzedAt)
    {
        _analyzedAt = analyzedAt;
        return this;
    }

    public LlmAnalysisResultBuilder WithSuccess(LlmAnalysisSuccess success)
    {
        _success = success;
        _failure = null; // Ensure only one is set
        return this;
    }

    public LlmAnalysisResultBuilder WithFailure(LlmAnalysisFailure failure)
    {
        _failure = failure;
        _success = null; // Ensure only one is set
        return this;
    }

    public LlmAnalysisResult Build()
    {
        if (_success != null)
        {
            return LlmAnalysisResult.ForSuccess(_analyzedAt, _success);
        }
        else if (_failure != null)
        {
            return LlmAnalysisResult.ForFailure(_analyzedAt, _failure);
        }
        else
        {
            // Default to success if neither is explicitly set
            return LlmAnalysisResult.ForSuccess(_analyzedAt, new LlmAnalysisSuccessBuilder().Build());
        }
    }
}
