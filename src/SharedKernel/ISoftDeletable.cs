namespace SharedKernel;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    void Delete(DateTime utcNow);
}
