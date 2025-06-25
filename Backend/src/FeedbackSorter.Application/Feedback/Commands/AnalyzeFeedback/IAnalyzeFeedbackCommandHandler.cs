using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback.Commands.AnalyzeFeedback;

public interface IAnalyzeFeedbackCommandHandler
{
    Task<Result> Handle(AnalyzeFeedbackCommand command, CancellationToken cancellationToken);
}
