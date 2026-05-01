using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Domain.Users.ValueObjects;
using SharedKernel;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandHandler(
    IPasswordHasher passwordHasher,
    IUserWriteRepository userWriteRepository,
    IApplicationDbContext applicationDbContext)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        bool userExists = await userWriteRepository.UserExists(command.Email, cancellationToken);
        if (userExists)
        {
            return Result.Failure<Guid>(UserErrors.EmailNotUnique);
        }

        Result<Email> emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<Guid>(emailResult.Error);
        }

        string hashedPassword = passwordHasher.Hash(command.Password);

        var user = User.Create(
            emailResult.Value,
            command.FirstName,
            command.LastName,
            hashedPassword);

        await userWriteRepository.AddAsync(user);

        // UserRegisteredDomainEvent is raised inside User.Create() — no manual Raise() needed here.
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
