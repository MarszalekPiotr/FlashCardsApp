using Domain.FlashcardCollection.DomainServices;
using Domain.FlashcardCollection.Enums;
using Domain.FlashcardCollection.Events;
using SharedKernel;

namespace Domain.FlashcardCollection;

public class Flashcard : Entity
{
    public Guid Id { get; private set; }
    public Guid FlashcardCollectionId { get; private set; }

    public string SentenceWithBlanks { get; private set; }
    public string Translation { get; private set; }
    public string Answer { get; private set; }
    public Synonyms Synonyms { get; private set; }

    public SrsState SrsState { get; private set; }

    private readonly List<FlashcardReview> _reviews = new();
    public IReadOnlyCollection<FlashcardReview> Reviews => _reviews.AsReadOnly();

    private Flashcard() { } // Required by EF Core

    private Flashcard(Guid flashcardCollectionId, string sentenceWithBlanks, string translation, string answer, Synonyms synonyms, DateTime currentTime)
    {
        Id = Guid.CreateVersion7();
        FlashcardCollectionId = flashcardCollectionId;
        SentenceWithBlanks = sentenceWithBlanks;
        Translation = translation;
        Answer = answer;
        Synonyms = synonyms;

        SrsState = SrsState.CreateInitialState(Id, currentTime);
    }

    public static Flashcard Create(Guid flashcardCollectionId, string sentenceWithBlanks, string translation, string answer, Synonyms synonyms, DateTime currentTime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sentenceWithBlanks, nameof(sentenceWithBlanks));
        ArgumentException.ThrowIfNullOrWhiteSpace(translation, nameof(translation));
        ArgumentException.ThrowIfNullOrWhiteSpace(answer, nameof(answer));
        ArgumentNullException.ThrowIfNull(synonyms);

        return new Flashcard(flashcardCollectionId, sentenceWithBlanks, translation, answer, synonyms, currentTime);
    }

    public void Update(string sentenceWithBlanks, string translation, string answer, Synonyms synonyms)
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

        SentenceWithBlanks = sentenceWithBlanks;
        Translation = translation;
        Answer = answer;
        Synonyms = synonyms;
    }

    public FlashcardReview AddReview(ReviewResult reviewResult, SrsCalculationService srsCalculationService, DateTime currentTime)
    {
        ArgumentNullException.ThrowIfNull(srsCalculationService);

        var review = FlashcardReview.Create(Id, currentTime, reviewResult);
        _reviews.Add(review);

        SrsStateCalculation newSrsState = srsCalculationService.CalculateNextState(SrsState, reviewResult, currentTime);
        SrsState.UpdateState(newSrsState);

        Raise(new FlashcardReviewedDomainEvent(review.Id, FlashcardCollectionId, Id, review.ReviewDate, review.ReviewResult));

        return review;
    }
}
