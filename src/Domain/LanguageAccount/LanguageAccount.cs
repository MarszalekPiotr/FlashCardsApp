using System;
using System.Collections.Generic;
using System.Text;
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

    private readonly List<Flashcard> _flashcards = new();
    public IReadOnlyCollection<Flashcard> Flashcards => _flashcards.AsReadOnly();

    public LanguageAccount(Guid userId, ProficiencyLevel proficiencyLevel, Language language)
    {
        ArgumentNullException.ThrowIfNull(proficiencyLevel);
        ArgumentNullException.ThrowIfNull(language);

        Id = Guid.NewGuid();
        UserId = userId;
        ProficiencyLevel = proficiencyLevel;
        Language = language;

        Raise(new LanguageAccountCreatedDomainEvent(Id));
    }

    public Flashcard CreateFlashcard(string sentenceWithBlanks, string translation, string answer, Synonyms synonyms)
    {
        var flashcard = new Flashcard(Id, sentenceWithBlanks, translation, answer, synonyms);
        _flashcards.Add(flashcard);

        Raise(new FlashcardCreatedDomainEvent(flashcard.Id));

        return flashcard;
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
