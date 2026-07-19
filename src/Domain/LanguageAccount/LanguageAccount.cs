using Domain.LanguageAccount.Events;
using Domain.LanguageAccount.ValueObjects;
using Domain.Users;
using SharedKernel;

namespace Domain.LanguageAccount;

public class LanguageAccount : Entity, ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    public ProficiencyLevel ProficiencyLevel { get; private set; }
    public Guid LanguageId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string Name {  get; private set; }

    private LanguageAccount() { } // Required by EF Core

    private LanguageAccount(Guid userId, ProficiencyLevel proficiencyLevel, Guid languageId, string name)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        ProficiencyLevel = proficiencyLevel;
        LanguageId = languageId;
        Name = name;
    }

    

    public static LanguageAccount Create(Guid userId, ProficiencyLevel proficiencyLevel, Guid languageId, string name)
    {
        ArgumentNullException.ThrowIfNull(proficiencyLevel);    
        ArgumentNullException.ThrowIfNull(name);
        return new LanguageAccount(userId, proficiencyLevel, languageId, name);
    }

    public void UpdateProficiencyLevel(ProficiencyLevel newLevel)
    {
        ArgumentNullException.ThrowIfNull(newLevel);

        ProficiencyLevel = newLevel;
    }

    public void Delete(DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAt = utcNow;
    }
}
