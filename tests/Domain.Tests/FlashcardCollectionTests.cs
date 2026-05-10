using Shouldly;
using FC = Domain.FlashcardCollection.FlashcardCollection;

namespace Domain.Tests;

public sealed class FlashcardCollectionTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidInputs_SetsProperties()
    {
        var c = FC.Create(AccountId, "German Basics");

        c.Name.ShouldBe("German Basics");
        c.LanguageAccountId.ShouldBe(AccountId);
        c.IsDeleted.ShouldBeFalse();
        c.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_IdIsNotEmpty()
    {
        var c = FC.Create(AccountId, "Test");

        c.Id.ShouldNotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_NullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => FC.Create(AccountId, name!));
    }

    [Fact]
    public void Create_RaisesFlashcardCollectionCreatedDomainEvent()
    {
        var c = FC.Create(AccountId, "Test");

        c.DomainEvents.ShouldContain(e => e.GetType().Name == "FlashcardCollectionCreatedDomainEvent");
    }

    // ── Rename ────────────────────────────────────────────────────────────────

    [Fact]
    public void Rename_ValidName_UpdatesName()
    {
        var c = FC.Create(AccountId, "Old");

        c.Rename("New Name");

        c.Name.ShouldBe("New Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WhitespaceName_ThrowsArgumentException(string name)
    {
        var c = FC.Create(AccountId, "Test");

        Should.Throw<ArgumentException>(() => c.Rename(name));
    }

    // ── ISoftDeletable ────────────────────────────────────────────────────────

    [Fact]
    public void Delete_SetsIsDeletedAndDeletedAt()
    {
        var c = FC.Create(AccountId, "Test");
        var deletedAt = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);

        c.Delete(deletedAt);

        c.IsDeleted.ShouldBeTrue();
        c.DeletedAt.ShouldBe(deletedAt);
    }
}
