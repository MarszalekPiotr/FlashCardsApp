
using Domain.FlashcardCollection.Enums;
using Domain.FlashcardCollection.Events;
using SharedKernel;

namespace Domain.FlashcardCollection;

public class FlashcardReview : Entity
{
    public Guid Id { get; private set; }
    public Guid FlashcardId { get; private set; }
    public DateTime ReviewDate { get; private set; }
    public ReviewResult ReviewResult { get; private set; }

    private FlashcardReview() { } // Required by EF Core

    private FlashcardReview(Guid flashcardId, DateTime reviewDate, ReviewResult reviewResult)
    {
        Id = Guid.CreateVersion7();
        FlashcardId = flashcardId;
        ReviewDate = reviewDate;
        ReviewResult = reviewResult;
    }

    internal static FlashcardReview Create(Guid flashcardId, DateTime reviewDate, ReviewResult reviewResult)
    {
        var review = new FlashcardReview(flashcardId, reviewDate, reviewResult);
        return review;
    }
}
