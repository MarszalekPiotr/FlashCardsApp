using System;
using Domain.SRS.ValueObjects;
using SharedKernel;

namespace Domain.SRS;

public class FlashcardReview : Entity
{
    public Guid Id { get; private set; }
    public Guid FlashcardId { get; private set; }
    public DateTime ReviewDate { get; private set; }
    public ReviewResult ReviewResult { get; private set; }

    internal FlashcardReview(Guid flashcardId, DateTime reviewDate, ReviewResult reviewResult)
    {
        if (reviewDate > DateTime.UtcNow)
        {
            throw new ArgumentException("Review date cannot be in the future.", nameof(reviewDate));
        }

        ArgumentNullException.ThrowIfNull(reviewResult);

        //Id = Guid.NewGuid();
        FlashcardId = flashcardId;
        ReviewDate = reviewDate;
        ReviewResult = reviewResult;
    }
}
