using Domain.LanguageAccount.Events;
using Domain.LanguageAccount.ValueObjects;
using Domain.Users;
using SharedKernel;

namespace Domain.LanguageAccount;

public class LanguageAccount : Entity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    public ProficiencyLevel ProficiencyLevel { get; private set; }
    public Guid LanguageId { get; private set; }

    private LanguageAccount() { } // Required by EF Core

    private LanguageAccount(Guid userId, ProficiencyLevel proficiencyLevel, Guid languageId)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        ProficiencyLevel = proficiencyLevel;
        LanguageId = languageId;

    }

    public static LanguageAccount Create(Guid userId, ProficiencyLevel proficiencyLevel, Guid languageId)
    {
        ArgumentNullException.ThrowIfNull(proficiencyLevel);    
        return new LanguageAccount(userId, proficiencyLevel, languageId);
    }

    public void UpdateProficiencyLevel(ProficiencyLevel newLevel)
    {
        ArgumentNullException.ThrowIfNull(newLevel);      

        ProficiencyLevel = newLevel;
    }
}
