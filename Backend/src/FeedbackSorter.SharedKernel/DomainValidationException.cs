namespace FeedbackSorter.SharedKernel;

public class DomainValidationException : DomainException
{
    public DomainValidationException()
        : base("One or more validation failures have occurred.")
    {
    }

    public DomainValidationException(string description)
        : base(description)
    {
    }

    public static DomainValidationException ForArgument(string? description, string argument)
    {
        string message;
        if (description is null)
        {
            message = "Argument " + argument + " is invalid";
        }
        else
        {
            message = description + "; Argument: " + argument;
        }
        return new DomainValidationException(message);
    }
}
