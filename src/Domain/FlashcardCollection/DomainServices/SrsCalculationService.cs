using System;
using Domain.FlashcardCollection.Enums;

namespace Domain.FlashcardCollection.DomainServices;

/// <summary>
/// Domain service responsible for calculating SRS (Spaced Repetition System) state
/// based on the SuperMemo SM-2 algorithm.
/// </summary>
public sealed class SrsCalculationService
{
    private const double MinEaseFactor = 1.3;

    /// <summary>
    /// Calculates the next SRS state based on the review result.
    /// </summary>
    /// <param name="currentState">The current SRS state</param>
    /// <param name="reviewResult">The result of the flashcard review</param>
    /// <param name="currentTime">The current time (for testability)</param>
    /// <returns>A new SRS state calculation with updated values</returns>
    public SrsStateCalculation CalculateNextState(
        SrsState currentState, 
        ReviewResult reviewResult, 
        DateTime currentTime)
    {
        ArgumentNullException.ThrowIfNull(currentState);

        int newInterval;
        double newEaseFactor = currentState.EaseFactor;
        int newRepetitions;

        // Failed review - reset progress
        if (reviewResult is ReviewResult.Again or ReviewResult.DontKnow)
        {
            newRepetitions = 0;
            newInterval = 1;
            newEaseFactor = Math.Max(MinEaseFactor, currentState.EaseFactor - 0.2);
        }
        // Successful review - increase interval
        else
        {
            newRepetitions = currentState.Repetitions + 1;

            // Calculate new interval based on repetition count
            if (newRepetitions == 1)
            {
                newInterval = 1;
            }
            else if (newRepetitions == 2)
            {
                newInterval = 3;
            }
            else
            {
                newInterval = (int)Math.Round(currentState.Interval * currentState.EaseFactor);
            }

            // Adjust ease factor based on review quality
            if (reviewResult is ReviewResult.Easy)
            {
                newEaseFactor += 0.15;
            }
            else if (reviewResult is ReviewResult.Know)
            {
                newEaseFactor += 0.05;
            }
        }

        // Ensure ease factor doesn't go below minimum
        newEaseFactor = Math.Max(MinEaseFactor, newEaseFactor);
        
        // Calculate next review date
        DateTime nextReviewDate = currentTime.AddDays(newInterval);

        return new SrsStateCalculation(
            newInterval,
            newEaseFactor,
            newRepetitions,
            nextReviewDate);
    }
}

/// <summary>
/// Data Transfer Object containing the result of SRS state calculation.
/// This is an immutable record representing the new state after a review.
/// </summary>
public sealed record SrsStateCalculation(
    int Interval,
    double EaseFactor,
    int Repetitions,
    DateTime NextReviewDate)
{
    /// <summary>
    /// Validates that the calculated values are within acceptable ranges.
    /// </summary>
    public bool IsValid() =>
        Interval > 0 &&
        EaseFactor >= 1.3 &&
        Repetitions >= 0 &&
        NextReviewDate > DateTime.MinValue;
}
