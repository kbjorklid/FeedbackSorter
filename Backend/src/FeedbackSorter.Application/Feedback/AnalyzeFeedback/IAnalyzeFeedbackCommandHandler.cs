using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback.AnalyzeFeedback;

public interface IAnalyzeFeedbackCommandHandler
{
    Task<Result> Handle(AnalyzeFeedbackCommand command, CancellationToken cancellationToken);
}
