using Domain.SRS.Events;
using Domain.SRS.ValueObjects;
using SharedKernel;

namespace Domain.SRS;

public class FlashcardReview : Entity
{
    public Guid Id { get; private set; }
    public Guid FlashcardId { get; private set; }
    public DateTime ReviewDate { get; private set; }
    public ReviewResult ReviewResult { get; private set; }

    private FlashcardReview() { } // Required by EF Core

    private FlashcardReview(Guid flashcardId, DateTime reviewDate, ReviewResult reviewResult)
    {
        if (reviewDate > DateTime.UtcNow)
            throw new ArgumentException("Review date cannot be in the future.", nameof(reviewDate));

        ArgumentNullException.ThrowIfNull(reviewResult);

        FlashcardId = flashcardId;
        ReviewDate = reviewDate;
        ReviewResult = reviewResult;
    }

    public static FlashcardReview Create(Guid flashcardId, DateTime reviewDate, ReviewResult reviewResult)
    {
        var review = new FlashcardReview(flashcardId, reviewDate, reviewResult);
        review.Raise(new FlashcardReviewedDomainEvent(review.Id, flashcardId, (int)reviewResult.Value));
        return review;
    }
}
