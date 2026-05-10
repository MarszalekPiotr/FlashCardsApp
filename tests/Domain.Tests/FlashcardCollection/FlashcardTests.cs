using Domain.FlashcardCollection;
using Domain.FlashcardCollection.DomainServices;
using Domain.FlashcardCollection.Enums;
using Shouldly;

namespace Domain.Tests.FlashcardCollection;

public sealed class FlashcardTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid CollectionId = Guid.NewGuid();
    private readonly SrsCalculationService _srs = new();

    private static Flashcard CreateDefault() =>
        Flashcard.Create(CollectionId, "I ___ yesterday", "Byłem tu wczoraj", "was", new Synonyms([]), T0);

    // ── Flashcard.Create() guards ─────────────────────────────────────────────

    [Theory]
    [InlineData("", "translation", "answer")]
    [InlineData("sentence", "", "answer")]
    [InlineData("sentence", "translation", "")]
    public void Create_EmptyRequiredField_ThrowsArgumentException(
        string sentence, string translation, string answer)
    {
        Should.Throw<ArgumentException>(() =>
            Flashcard.Create(CollectionId, sentence, translation, answer, new Synonyms([]), T0));
    }

    [Fact]
    public void Create_NullSynonyms_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            Flashcard.Create(CollectionId, "sentence", "translation", "answer", null!, T0));
    }

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Create_InitialSrsState_HasZeroIntervalAndRepetitions()
    {
        var f = CreateDefault();

        f.SrsState.Interval.ShouldBe(0);
        f.SrsState.Repetitions.ShouldBe(0);
        f.SrsState.EaseFactor.ShouldBe(2.5);
    }

    [Fact]
    public void Create_InitialReviews_IsEmpty()
    {
        var f = CreateDefault();

        f.Reviews.ShouldBeEmpty();
    }

    [Fact]
    public void Create_IdIsNotEmpty()
    {
        var f = CreateDefault();

        f.Id.ShouldNotBe(Guid.Empty);
    }

    // ── AddReview() ───────────────────────────────────────────────────────────

    [Fact]
    public void AddReview_Know_UpdatesSrsStateAndAddsReview()
    {
        var f = CreateDefault();

        f.AddReview(ReviewResult.Know, _srs, T0);

        f.Reviews.Count.ShouldBe(1);
        f.SrsState.Repetitions.ShouldBe(1);
        f.SrsState.Interval.ShouldBe(1);
        f.SrsState.NextReviewDate.ShouldBe(T0.AddDays(1));
    }

    [Fact]
    public void AddReview_Again_ResetsProgressAndAddsReview()
    {
        var f = CreateDefault();
        f.AddReview(ReviewResult.Know, _srs, T0);
        f.AddReview(ReviewResult.Again, _srs, T0.AddDays(1));

        f.Reviews.Count.ShouldBe(2);
        f.SrsState.Repetitions.ShouldBe(0);
        f.SrsState.Interval.ShouldBe(1);
    }

    [Fact]
    public void AddReview_MultipleReviews_AccumulateInList()
    {
        var f = CreateDefault();

        f.AddReview(ReviewResult.Know, _srs, T0);
        f.AddReview(ReviewResult.Easy, _srs, T0.AddDays(1));
        f.AddReview(ReviewResult.Know, _srs, T0.AddDays(4));

        f.Reviews.Count.ShouldBe(3);
    }

    [Fact]
    public void AddReview_RaisesFlashcardReviewedDomainEvent()
    {
        var f = CreateDefault();

        f.AddReview(ReviewResult.Easy, _srs, T0);

        f.DomainEvents.ShouldContain(e => e.GetType().Name == "FlashcardReviewedDomainEvent");
    }

    [Fact]
    public void AddReview_NullSrsService_ThrowsArgumentNullException()
    {
        var f = CreateDefault();

        Should.Throw<ArgumentNullException>(() => f.AddReview(ReviewResult.Know, null!, T0));
    }
}
