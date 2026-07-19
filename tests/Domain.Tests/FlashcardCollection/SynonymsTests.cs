using Domain.FlashcardCollection;
using Shouldly;

namespace Domain.Tests.FlashcardCollection;

public sealed class SynonymsTests
{
    // ── Empty list ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_EmptyList_IsAllowed()
    {
        var s = new Synonyms([]);

        s.Value.ShouldBeEmpty();
    }

    // ── Valid lists ───────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidList_StoresAllValues()
    {
        var s = new Synonyms(["walked", "traveled", "went"]);

        s.Value.ShouldBe(["walked", "traveled", "went"]);
    }

    [Fact]
    public void Create_SingleItem_IsAllowed()
    {
        var s = new Synonyms(["walked"]);

        s.Value.ShouldHaveSingleItem();
    }

    // ── Null guard ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_NullList_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new Synonyms(null!));
    }

    // ── Whitespace guard ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhitespaceSynonym_ThrowsArgumentException(string whitespace)
    {
        Should.Throw<ArgumentException>(() => new Synonyms(["valid", whitespace]));
    }

    // ── Uniqueness (case-insensitive) ─────────────────────────────────────────

    [Fact]
    public void Create_ExactDuplicate_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new Synonyms(["walk", "walk"]));
    }

    [Fact]
    public void Create_CaseInsensitiveDuplicate_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new Synonyms(["walk", "WALK"]));
    }

    [Fact]
    public void Create_MixedCaseDuplicate_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new Synonyms(["Walk", "wAlK"]));
    }

}
