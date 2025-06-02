using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.MarkAnalyzed;

public record MarkFeedbackAnalyzedCommand(FeedbackId UserFeedbackId, LlmAnalysisResult LlmAnalysisResult);
