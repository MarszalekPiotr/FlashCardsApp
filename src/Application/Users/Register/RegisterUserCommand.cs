using Application.Abstractions.Messaging;
using Domain.Users.ValueObjects;

namespace Application.Users.Register;

public sealed record RegisterUserCommand(string Email, string FirstName, string LastName, string Password)
    : ICommand<Guid>;
