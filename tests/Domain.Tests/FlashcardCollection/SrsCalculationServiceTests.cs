using Domain.FlashcardCollection;
using Domain.FlashcardCollection.DomainServices;
using Domain.FlashcardCollection.Enums;
using Shouldly;

namespace Domain.Tests.FlashcardCollection;

public sealed class SrsCalculationServiceTests
{
    private readonly SrsCalculationService _sut = new();
    private readonly DateTime _t0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private SrsState FreshState() => SrsState.CreateInitialState(Guid.NewGuid(), _t0);

    // ── First review ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ReviewResult.Easy)]
    [InlineData(ReviewResult.Know)]
    public void FirstReview_Success_SetsIntervalTo1AndRepetitionsTo1(ReviewResult result)
    {
        var state = FreshState();
        var calc = _sut.CalculateNextState(state, result, _t0);

        calc.Interval.ShouldBe(1);
        calc.Repetitions.ShouldBe(1);
        calc.NextReviewDate.ShouldBe(_t0.AddDays(1));
    }

    // ── Interval progression (SM-2 standard sequence) ────────────────────────

    [Fact]
    public void SecondSuccessfulReview_SetsIntervalTo3()
    {
        var state = FreshState();
        state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Know, _t0));

        var r2 = _sut.CalculateNextState(state, ReviewResult.Know, _t0.AddDays(1));

        r2.Interval.ShouldBe(3);
        r2.Repetitions.ShouldBe(2);
    }

    [Fact]
    public void ThirdSuccessfulReview_UsesEaseFactorMultiplier()
    {
        var state = FreshState();
        state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Know, _t0));
        state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Know, _t0.AddDays(1)));

        // state.Interval == 3, state.EaseFactor == 2.6 at this point
        double easeBeforeThird = state.EaseFactor;
        int intervalBeforeThird = state.Interval;
        var r3 = _sut.CalculateNextState(state, ReviewResult.Know, _t0.AddDays(4));

        int expected = (int)Math.Round(intervalBeforeThird * easeBeforeThird);
        r3.Interval.ShouldBe(expected);
        r3.Repetitions.ShouldBe(3);
    }

    // ── Failed review — resets progress ──────────────────────────────────────

    [Theory]
    [InlineData(ReviewResult.Again)]
    [InlineData(ReviewResult.DontKnow)]
    public void FailedReview_ResetsIntervalAndRepetitionsToOne(ReviewResult failResult)
    {
        var state = FreshState();
        // Advance to repetitions = 3
        state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Know, _t0));
        state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Know, _t0.AddDays(1)));
        state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Easy, _t0.AddDays(4)));

        var calc = _sut.CalculateNextState(state, failResult, _t0.AddDays(10));

        calc.Repetitions.ShouldBe(0);
        calc.Interval.ShouldBe(1);
    }

    // ── Ease factor adjustments ───────────────────────────────────────────────

    [Fact]
    public void Easy_IncreasesEaseFactorBy0_15()
    {
        var state = FreshState();
        double original = state.EaseFactor;

        var calc = _sut.CalculateNextState(state, ReviewResult.Easy, _t0);

        calc.EaseFactor.ShouldBe(original + 0.15, tolerance: 0.001);
    }

    [Fact]
    public void Know_IncreasesEaseFactorBy0_05()
    {
        var state = FreshState();
        double original = state.EaseFactor;

        var calc = _sut.CalculateNextState(state, ReviewResult.Know, _t0);

        calc.EaseFactor.ShouldBe(original + 0.05, tolerance: 0.001);
    }

    [Theory]
    [InlineData(ReviewResult.Again)]
    [InlineData(ReviewResult.DontKnow)]
    public void Fail_DecreasesEaseFactorBy0_20(ReviewResult failResult)
    {
        var state = FreshState();
        double expected = Math.Max(1.3, state.EaseFactor - 0.2);

        var calc = _sut.CalculateNextState(state, failResult, _t0);

        calc.EaseFactor.ShouldBe(expected, tolerance: 0.001);
    }

    // ── Ease factor minimum clamp ─────────────────────────────────────────────

    [Fact]
    public void EaseFactor_NeverDropsBelowMinimum_After20Failures()
    {
        var state = FreshState();
        for (int i = 0; i < 20; i++)
        {
            state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Again, _t0.AddDays(i)));
        }

        state.EaseFactor.ShouldBeGreaterThanOrEqualTo(1.3);
    }

    // ── NextReviewDate ────────────────────────────────────────────────────────

    [Fact]
    public void NextReviewDate_IsReviewTimePlusInterval()
    {
        var state = FreshState();
        var reviewTime = new DateTime(2026, 6, 15, 9, 30, 0, DateTimeKind.Utc);

        var calc = _sut.CalculateNextState(state, ReviewResult.Know, reviewTime);

        calc.NextReviewDate.ShouldBe(reviewTime.AddDays(calc.Interval));
    }

    // ── Guard clauses ─────────────────────────────────────────────────────────

    [Fact]
    public void NullState_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => _sut.CalculateNextState(null!, ReviewResult.Know, _t0));
    }

    // ── SrsStateCalculation.IsValid() ─────────────────────────────────────────

    [Fact]
    public void AllReviewResults_ProduceValidCalculation()
    {
        var state = FreshState();

        foreach (ReviewResult r in Enum.GetValues<ReviewResult>())
        {
            _sut.CalculateNextState(state, r, _t0).IsValid().ShouldBeTrue();
        }
    }
}
