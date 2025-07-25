namespace FeedbackSorter.SharedKernel;

/// <summary>
/// Base class for all entities in the domain.
/// </summary>
public abstract class Entity<TId>
{
    public TId Id { get; private set; }

    protected Entity(TId id)
    {
        Id = id;
    }
}
