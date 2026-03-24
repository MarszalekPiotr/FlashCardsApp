using System;
using System.Collections.Generic;
using System.Text;
using Domain.LanguageAccount.Events;
using Domain.LanguageAccount.ValueObjects;
using SharedKernel;

namespace Domain.LanguageAccount;

public class Flashcard : Entity
{
    public Guid Id { get; private set; }
    public Guid LanguageAccountId { get; private set; }
    public LanguageAccount? LanguageAccount { get; private set; }

    public string SentenceWithBlanks { get; private set; }
    public string Translation { get; private set; }
    public string Answer { get; private set; }
    public Synonyms Synonyms { get; private set; }

    private readonly List<FlashcardReview> _flashcardReviews = new();
    public IReadOnlyCollection<FlashcardReview> FlashcardReviews => _flashcardReviews.AsReadOnly();

    internal Flashcard(Guid languageAccountId, string sentenceWithBlanks, string translation, string answer, Synonyms synonyms)
    {
        if (string.IsNullOrWhiteSpace(sentenceWithBlanks))
        {
            throw new ArgumentException("Sentence with blanks cannot be null or whitespace.", nameof(sentenceWithBlanks));
        }
        if (string.IsNullOrWhiteSpace(translation))
        {
            throw new ArgumentException("Translation cannot be null or whitespace.", nameof(translation));
        }
        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new ArgumentException("Answer cannot be null or whitespace.", nameof(answer));
        }

        ArgumentNullException.ThrowIfNull(synonyms);

        Id = Guid.NewGuid();
        LanguageAccountId = languageAccountId;
        SentenceWithBlanks = sentenceWithBlanks;
        Translation = translation;
        Answer = answer;
        Synonyms = synonyms;
    }

    public FlashcardReview Review(ReviewResult reviewResult)
    {
        ArgumentNullException.ThrowIfNull(reviewResult);

        var review = new FlashcardReview(Id, DateTime.UtcNow, reviewResult);
        _flashcardReviews.Add(review);

        Raise(new FlashcardReviewedDomainEvent(review.Id));

        return review;
    }
}
