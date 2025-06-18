namespace FeedbackSorter.SharedKernel;


public class DomainException : Exception
{

    public DomainException(string description)
        : base(description)
    {
    }

    public DomainException(string description, Exception exception) : base(description, exception)
    {
    }
}
