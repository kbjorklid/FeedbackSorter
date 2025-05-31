

using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

public record LLMAnalysisResult
{
    public required FeedbackTitle Title { get; init; }
    public required Sentiment Sentiment { get; init; }
    public required ISet<FeedbackCategoryType> FeedbackCategories { get; init; }
    public required ISet<string> FeatureCategoryNames { get; init; }
    public required Timestamp AnalyzedAt { get; init; }
}
