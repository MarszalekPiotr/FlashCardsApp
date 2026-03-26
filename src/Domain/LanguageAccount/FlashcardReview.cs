using System;
using System.Collections.Generic;
using System.Text;
using Domain.LanguageAccount.ValueObjects;
using SharedKernel;

namespace Domain.LanguageAccount;

public class FlashcardReview : Entity
{
    public Guid Id { get; private set; }
    public Guid FlashcardId { get; private set; }
    public Flashcard? Flashcard { get; private set; }
    public DateTime ReviewDate { get; private set; }
    public ReviewResult ReviewResult { get; private set; }

    private FlashcardReview() { } // Required by EF Core

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
