using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.LLM;

public record LlmAnalysisResult
{
    public bool IsSuccess { get => Success != null; }
    public LlmAnalysisSuccess? Success { get; init; }
    public LlmAnalysisFailure? Failure { get; init; }
    public Timestamp AnalyzedAt { get; init; }

    private LlmAnalysisResult(Timestamp AnalyzedAt, LlmAnalysisSuccess? Success = null, LlmAnalysisFailure? Failure = null)
    {
        this.Success = Success;
        this.Failure = Failure;
        this.AnalyzedAt = AnalyzedAt;
    }

    public static LlmAnalysisResult ForSuccess(Timestamp analyzedAt, LlmAnalysisSuccess success)
    {
        return new LlmAnalysisResult(analyzedAt, success);
    }

    public static LlmAnalysisResult ForFailure(Timestamp analyzedAt, LlmAnalysisFailure failure)
    {
        return new LlmAnalysisResult(analyzedAt, Failure: failure);
    }
}


public record LlmAnalysisSuccess
{
    public required FeedbackTitle Title { get; init; }
    public required Sentiment Sentiment { get; init; }
    public required ISet<FeedbackCategoryType> FeedbackCategories { get; init; }
    public required ISet<string> FeatureCategoryNames { get; init; }
}

public record LlmAnalysisFailure
{
    public required string Error { get; init; }

    public required FailureReason Reason { get; init; }
}
