using System;
using SharedKernel;
using Domain.FlashcardCollection.Enums;
using Domain.FlashcardCollection.DomainServices;

namespace Domain.FlashcardCollection;

public class SrsState : Entity
{
    public Guid FlashcardId { get; private set; }
    public int Interval { get; private set; } // in days
    public double EaseFactor { get; private set; }
    public int Repetitions { get; private set; }
    public DateTime NextReviewDate { get; private set; }

    private SrsState() { } // Required by EF Core

    private SrsState(Guid flashcardId, int interval, double easeFactor, int repetitions, DateTime nextReviewDate)
    {
        FlashcardId = flashcardId;
        Interval = interval;
        EaseFactor = easeFactor;
        Repetitions = repetitions;
        NextReviewDate = nextReviewDate;
    }

    public static SrsState CreateInitialState(Guid flashcardId, DateTime currentTime)
    {
        return new SrsState(
            flashcardId,
            interval: 0,
            easeFactor: 2.5,
            repetitions: 0,
            nextReviewDate: currentTime);
    }

    public void UpdateState(SrsStateCalculation srsStateCalculation)
    {
       Interval = srsStateCalculation.Interval;
       EaseFactor = srsStateCalculation.EaseFactor;
       Repetitions = srsStateCalculation.Repetitions;
       NextReviewDate = srsStateCalculation.NextReviewDate;

    }
}
