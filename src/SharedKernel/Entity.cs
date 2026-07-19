using System.ComponentModel.DataAnnotations;

namespace SharedKernel;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public List<IDomainEvent> DomainEvents => [.. _domainEvents];

    /// <summary>
    /// Optimistic concurrency token. EF Core maps this to a SQL Server rowversion column.
    /// On every concurrent UPDATE/DELETE, EF Core checks this value hasn't changed since
    /// the entity was loaded. If it has, DbUpdateConcurrencyException is thrown.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; protected set; }

    /// <summary>UTC timestamp set once when the entity is first created.</summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>UTC timestamp updated on every modification, null if never updated after creation.</summary>
    public DateTime? UpdatedAt { get; protected set; }

    protected Entity()
    {
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Called by the infrastructure layer (ApplicationDbContext) before every SaveChanges
    /// to stamp the last-modification time. Not intended to be called from domain code.
    /// </summary>
    public void SetUpdatedAt(DateTime utcNow)
    {
        UpdatedAt = utcNow;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
