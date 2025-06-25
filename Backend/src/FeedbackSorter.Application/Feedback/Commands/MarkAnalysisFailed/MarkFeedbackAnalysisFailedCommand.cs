using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.Commands.MarkAnalysisFailed;

public record MarkFeedbackAnalysisFailedCommand(FeedbackId FeedbackId, LlmAnalysisResult LlmAnalysisResult);
