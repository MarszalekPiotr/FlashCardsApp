using Application.FlashcardCollection.Commands.AddFlashcardReview;
using FluentValidation.TestHelper;

namespace Application.Tests.FlashcardCollection;

public sealed class AddFlashcardReviewCommandValidatorTests
{
    private readonly AddFlashcardReviewCommandValidator _validator = new();

    // ReviewResult is stored as int in the command; valid values are 1–4 (Again/DontKnow/Know/Easy)
    private static AddFlashcardReviewCommand ValidCommand() => new(
        Guid.NewGuid(), Guid.NewGuid(), 3 /* Know */);

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1)] // Again
    [InlineData(2)] // DontKnow
    [InlineData(3)] // Know
    [InlineData(4)] // Easy
    public void AllValidReviewResults_PassValidation(int reviewResult)
    {
        var cmd = ValidCommand() with { ReviewResult = reviewResult };
        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(c => c.ReviewResult);
    }

    // ── FlaschardCollectionId (note: typo is intentional — matches command) ──

    [Fact]
    public void EmptyCollectionId_FailsValidation()
    {
        var cmd = ValidCommand() with { FlaschardCollectionId = Guid.Empty };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.FlaschardCollectionId);
    }

    // ── FlashcardId ───────────────────────────────────────────────────────────

    [Fact]
    public void EmptyFlashcardId_FailsValidation()
    {
        var cmd = ValidCommand() with { FlashcardId = Guid.Empty };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.FlashcardId);
    }

    // ── ReviewResult ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]   // not defined in enum
    [InlineData(999)] // out of range
    [InlineData(-1)]  // negative
    public void InvalidReviewResult_FailsValidation(int reviewResult)
    {
        var cmd = ValidCommand() with { ReviewResult = reviewResult };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.ReviewResult);
    }
}
