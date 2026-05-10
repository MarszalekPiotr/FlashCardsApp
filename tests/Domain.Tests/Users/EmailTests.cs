using Domain.Users.ValueObjects;
using Shouldly;

namespace Domain.Tests.Users;

public sealed class EmailTests
{
    // ── Valid inputs ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name+tag@sub.domain.io")]
    public void Create_ValidEmail_ReturnsSuccess(string email)
    {
        var result = Email.Create(email);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(email);
    }

    [Fact]
    public void Create_UppercaseEmail_ReturnsSuccess()
    {
        // EmailRegex uses RegexOptions.IgnoreCase
        var result = Email.Create("USER@EXAMPLE.COM");

        result.IsSuccess.ShouldBeTrue();
    }

    // ── Null / whitespace ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_NullOrWhitespace_ReturnsEmptyError(string? email)
    {
        var result = Email.Create(email!);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Email.Empty");
    }

    // ── Invalid format ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    public void Create_InvalidFormat_ReturnsInvalidFormatError(string email)
    {
        var result = Email.Create(email);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Email.InvalidFormat");
    }

    // ── Value equality (record semantics) ─────────────────────────────────────

    [Fact]
    public void TwoEmailsWithSameValue_AreEqual()
    {
        var a = Email.Create("user@example.com").Value;
        var b = Email.Create("user@example.com").Value;

        a.ShouldBe(b);
    }

    [Fact]
    public void TwoEmailsWithDifferentValues_AreNotEqual()
    {
        var a = Email.Create("a@example.com").Value;
        var b = Email.Create("b@example.com").Value;

        a.ShouldNotBe(b);
    }
}
