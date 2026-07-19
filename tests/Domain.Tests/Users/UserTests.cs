using Domain.Users;
using Domain.Users.ValueObjects;
using Shouldly;

namespace Domain.Tests.Users;

public sealed class UserTests
{
    private static Email ValidEmail() => Email.Create("user@example.com").Value;

    // ── User.Create() guards ──────────────────────────────────────────────────

    [Fact]
    public void Create_NullEmail_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            User.Create(null!, "Alice", "Smith", "hash"));
    }

    [Fact]
    public void Create_NullFirstName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            User.Create(ValidEmail(), null!, "Smith", "hash"));
    }

    [Fact]
    public void Create_NullLastName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            User.Create(ValidEmail(), "Alice", null!, "hash"));
    }

    [Fact]
    public void Create_NullPasswordHash_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            User.Create(ValidEmail(), "Alice", "Smith", null!));
    }

    [Fact]
    public void Create_ValidInputs_SetsProperties()
    {
        var user = User.Create(ValidEmail(), "Alice", "Smith", "hash");

        user.Email.Value.ShouldBe("user@example.com");
        user.FirstName.ShouldBe("Alice");
        user.LastName.ShouldBe("Smith");
        user.IsDeleted.ShouldBeFalse();
        user.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_IdIsNotEmpty()
    {
        var user = User.Create(ValidEmail(), "Alice", "Smith", "hash");

        user.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_RaisesUserRegisteredDomainEvent()
    {
        var user = User.Create(ValidEmail(), "Alice", "Smith", "hash");

        user.DomainEvents.ShouldContain(e => e.GetType().Name == "UserRegisteredDomainEvent");
    }

    // ── ISoftDeletable ────────────────────────────────────────────────────────

    [Fact]
    public void Delete_SetsIsDeletedTrueAndDeletedAt()
    {
        var user = User.Create(ValidEmail(), "Alice", "Smith", "hash");
        var deletedAt = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);

        user.Delete(deletedAt);

        user.IsDeleted.ShouldBeTrue();
        user.DeletedAt.ShouldBe(deletedAt);
    }

    [Fact]
    public void Delete_Twice_KeepsLastDeletedAt()
    {
        var user = User.Create(ValidEmail(), "Alice", "Smith", "hash");
        var first = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var second = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);

        user.Delete(first);
        user.Delete(second);

        user.DeletedAt.ShouldBe(second);
    }
}
