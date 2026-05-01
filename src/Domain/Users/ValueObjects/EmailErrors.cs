using SharedKernel;

namespace Domain.Users.ValueObjects;

public static class EmailErrors
{
    public static readonly Error Empty = Error.Validation(
        "Email.Empty",
        "Email address cannot be empty.");

    public static readonly Error InvalidFormat = Error.Validation(
        "Email.InvalidFormat",
        "Email address format is invalid.");
}
