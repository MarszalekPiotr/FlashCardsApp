using System.Text.RegularExpressions;
using SharedKernel;

namespace Domain.Users.ValueObjects;

public record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    // Private constructor — forces all callers through Create() or FromPersistence().
    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a validated Email value object.
    /// Returns a failure Result with a validation error if the format is invalid.
    /// Use this everywhere in application and domain code.
    /// </summary>
    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Email>(EmailErrors.Empty);
        }

        if (!EmailRegex.IsMatch(value))
        {
            return Result.Failure<Email>(EmailErrors.InvalidFormat);
        }

        return Result.Success(new Email(value));
    }

    /// <summary>
    /// Reconstructs an Email from a persisted (already validated) database value.
    /// Only for use by EF Core value converters in the Infrastructure layer.
    /// Do NOT call this from application or domain code.
    /// </summary>
    public static Email FromPersistence(string value) => new(value);

    public override string ToString() => Value;
}

