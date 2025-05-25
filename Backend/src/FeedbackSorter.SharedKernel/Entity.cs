namespace FeedbackSorter.SharedKernel;

/// <summary>
/// Base class for all entities in the domain.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }
}
