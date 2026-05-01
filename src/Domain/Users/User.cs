using Domain.Users.ValueObjects;
using SharedKernel;

namespace Domain.Users;

public sealed class User : Entity
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string PasswordHash { get; private set; }

    private User(Email email, string firstName, string lastName, string passwordHash)
    {
        Id = Guid.NewGuid();
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;

        // Domain guarantees: a UserRegisteredDomainEvent is always raised when a User is created,
        // regardless of which handler or service calls User.Create().
        Raise(new UserRegisteredDomainEvent(Id));
    }

    public static User Create(Email email, string firstName, string lastName, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(firstName);
        ArgumentNullException.ThrowIfNull(lastName);
        ArgumentNullException.ThrowIfNull(passwordHash);

        return new User(email, firstName, lastName, passwordHash);
    }
}
