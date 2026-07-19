using System;
using Domain.FlashcardCollection.Enums;

namespace Domain.FlashcardCollection.DomainServices;

/// <summary>
/// Domain service responsible for calculating SRS (Spaced Repetition System) state
/// based on the SuperMemo SM-2 algorithm.
/// Reference: https://www.supermemo.com/en/archives1990-2015/english/ol/sm2
/// </summary>
public sealed class SrsCalculationService
{
    // ── SM-2 Algorithm Constants ──────────────────────────────────────────────
    // Source: SuperMemo SM-2 paper. Quality values: q=5 (Easy), q=4 (Know), q<3 (Fail)

    /// <summary>Minimum allowed ease factor. SM-2 defines EF_min = 1.3</summary>
    private const double MinEaseFactor = 1.3;

    /// <summary>
    /// Ease factor bonus for "Easy" response (SM-2: q=5).
    /// Increases how quickly the interval grows on subsequent reviews.
    /// </summary>
    private const double EaseFactorBonusEasy = 0.15;

    /// <summary>
    /// Ease factor bonus for "Know" response (SM-2: q=4).
    /// Small positive reinforcement for correct recall.
    /// </summary>
    private const double EaseFactorBonusKnow = 0.05;

    /// <summary>
    /// Ease factor penalty for a failed review (SM-2: q less than 3).
    /// Applied when the user selects "Again" or "DontKnow".
    /// </summary>
    private const double EaseFactorPenaltyFail = 0.2;

    /// <summary>Interval in days after the 1st successful review (SM-2 standard: 1 day)</summary>
    private const int FirstSuccessInterval = 1;

    /// <summary>Interval in days after the 2nd successful review (SM-2 standard: 6 days, adjusted to 3)</summary>
    private const int SecondSuccessInterval = 3;

    /// <summary>Interval reset in days after a failed review (SM-2: restart from 1)</summary>
    private const int FailResetInterval = 1;

    /// <summary>
    /// Calculates the next SRS state based on the review result.
    /// </summary>
    /// <param name="currentState">The current SRS state</param>
    /// <param name="reviewResult">The result of the flashcard review</param>
    /// <param name="currentTime">The current time (injected for testability)</param>
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

        // Failed review — reset progress back to the beginning
        if (reviewResult is ReviewResult.Again or ReviewResult.DontKnow)
        {
            newRepetitions = 0;
            newInterval = FailResetInterval;
            newEaseFactor = Math.Max(MinEaseFactor, currentState.EaseFactor - EaseFactorPenaltyFail);
        }
        // Successful review — advance the interval
        else
        {
            newRepetitions = currentState.Repetitions + 1;

            newInterval = newRepetitions switch
            {
                1 => FirstSuccessInterval,
                2 => SecondSuccessInterval,
                _ => (int)Math.Round(currentState.Interval * currentState.EaseFactor)
            };

            if (reviewResult is ReviewResult.Easy)
                newEaseFactor += EaseFactorBonusEasy;
            else if (reviewResult is ReviewResult.Know)
                newEaseFactor += EaseFactorBonusKnow;
        }

        // Ensure ease factor never drops below the SM-2 minimum
        newEaseFactor = Math.Max(MinEaseFactor, newEaseFactor);

        return new SrsStateCalculation(
            newInterval,
            newEaseFactor,
            newRepetitions,
            currentTime.AddDays(newInterval));
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
