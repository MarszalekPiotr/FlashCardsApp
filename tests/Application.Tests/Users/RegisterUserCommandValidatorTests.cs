using Application.Users.Register;
using FluentValidation.TestHelper;

namespace Application.Tests.Users;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    private static RegisterUserCommand ValidCommand() => new(
        "user@example.com", "Alice", "Smith", "SecurePass1!");

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    // ── Email ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyEmail_FailsValidation(string? email)
    {
        var cmd = ValidCommand() with { Email = email! };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void InvalidEmailFormat_FailsValidation()
    {
        var cmd = ValidCommand() with { Email = "notanemail" };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Email);
    }

    // ── FirstName ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyFirstName_FailsValidation(string? name)
    {
        var cmd = ValidCommand() with { FirstName = name! };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.FirstName);
    }

    // ── LastName ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyLastName_FailsValidation(string? name)
    {
        var cmd = ValidCommand() with { LastName = name! };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.LastName);
    }

    // ── Password ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyPassword_FailsValidation(string? password)
    {
        var cmd = ValidCommand() with { Password = password! };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void PasswordTooShort_FailsValidation()
    {
        // MinimumLength(8) — "abc1234" is 7 chars
        var cmd = ValidCommand() with { Password = "abc1234" };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void PasswordExactlyMinLength_PassesValidation()
    {
        var cmd = ValidCommand() with { Password = "12345678" };
        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(c => c.Password);
    }
}
