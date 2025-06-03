using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.MarkAnalysisFailed;

public record MarkFeedbackAnalysisFailedCommand(FeedbackId FeedbackId, LlmAnalysisResult LlmAnalysisResult);
