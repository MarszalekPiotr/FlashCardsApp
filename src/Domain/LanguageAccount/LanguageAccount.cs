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

    private readonly List<FlashcardCollection> _flashcardCollections = new();
    public IReadOnlyCollection<FlashcardCollection> FlashcardCollections => _flashcardCollections.AsReadOnly();

    private LanguageAccount() { } // Required by EF Core

    private LanguageAccount(Guid userId, ProficiencyLevel proficiencyLevel, Guid languageId)
    {
        UserId = userId;
        ProficiencyLevel = proficiencyLevel;
        LanguageId = languageId;
        Raise(new LanguageAccountCreatedDomainEvent(Id));
    }

    public static LanguageAccount Create(Guid userId, ProficiencyLevel proficiencyLevel, Guid languageId)
    {
        ArgumentNullException.ThrowIfNull(proficiencyLevel);    
        return new LanguageAccount(userId, proficiencyLevel, languageId);
    }

    public FlashcardCollection CreateCollection(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
        }

        var collection = FlashcardCollection.Create(Id, name);
        _flashcardCollections.Add(collection);
        return collection;
    }

    public void UpdateProficiencyLevel(ProficiencyLevel newLevel)
    {
        ArgumentNullException.ThrowIfNull(newLevel);      

        ProficiencyLevel = newLevel;
    }
}
