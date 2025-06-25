using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Application.Feedback.Commands.MarkAnalyzed;

public record MarkFeedbackSuccessfullyAnalyzedCommand(FeedbackId UserFeedbackId, LlmAnalysisResult LlmAnalysisResult);
