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
    public Language Language { get; private set; }

    private readonly List<FlashcardCollection> _flashcardCollections = new();
    public IReadOnlyCollection<FlashcardCollection> FlashcardCollections => _flashcardCollections.AsReadOnly();

    private LanguageAccount() { } // Required by EF Core

    private LanguageAccount(Guid userId, ProficiencyLevel proficiencyLevel, Language language)
    {
        //Id = Guid.NewGuid();
        UserId = userId;
        ProficiencyLevel = proficiencyLevel;
        Language = language;
        Raise(new LanguageAccountCreatedDomainEvent(Id));
    }

    public static LanguageAccount Create(Guid userId, ProficiencyLevel proficiencyLevel, Language language)
    {
        ArgumentNullException.ThrowIfNull(proficiencyLevel);
        ArgumentNullException.ThrowIfNull(language);      
        return new LanguageAccount(userId, proficiencyLevel, language);
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

        if (newLevel.Value < ProficiencyLevel.Value)
        {
            throw new InvalidOperationException("Cannot downgrade proficiency level.");
        }

        ProficiencyLevel = newLevel;
    }
}
