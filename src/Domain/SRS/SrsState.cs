using System;
using SharedKernel;
using Domain.SRS.Enums;

namespace Domain.SRS;

public class SrsState : Entity
{
    public Guid FlashcardId { get; private set; }
    public int Interval { get; private set; } // in days
    public double EaseFactor { get; private set; }
    public int Repetitions { get; private set; }
    public DateTime NextReviewDate { get; private set; }

    private SrsState() { } // Required by EF Core

    public SrsState(Guid flashcardId, int interval, double easeFactor, int repetitions, DateTime nextReviewDate)
    {
        FlashcardId = flashcardId;
        Interval = interval;
        EaseFactor = easeFactor;
        Repetitions = repetitions;
        NextReviewDate = nextReviewDate;
    }

    public static SrsState CreateInitialState(Guid flashcardId)
    {
        return new SrsState(
            flashcardId,
            interval: 0,
            easeFactor: 2.5,
            repetitions: 0,
            nextReviewDate: DateTime.UtcNow);
    }

    public void UpdateState(Domain.SRS.ValueObjects.ReviewResult reviewResult)
    {
        const double minEaseFactor = 1.3;

        if (reviewResult.Value is ReviewResult.Again or ReviewResult.DontKnow)
        {
            Repetitions = 0;
            Interval = 1;
            EaseFactor = Math.Max(minEaseFactor, EaseFactor - 0.2);
        }
        else
        {
            Repetitions++;

            if (Repetitions == 1)
            {
                Interval = 1;
            }
            else if (Repetitions == 2)
            {
                Interval = 3;
            }
            else
            {
                Interval = (int)Math.Round(Interval * EaseFactor);
            }

            if (reviewResult.Value is ReviewResult.Easy)
            {
                EaseFactor += 0.15;
            }
            else if (reviewResult.Value is ReviewResult.Know)
            {
                EaseFactor += 0.05;
            }
        }

        EaseFactor = Math.Max(minEaseFactor, EaseFactor);
        NextReviewDate = DateTime.UtcNow.AddDays(Interval);
    }
}
