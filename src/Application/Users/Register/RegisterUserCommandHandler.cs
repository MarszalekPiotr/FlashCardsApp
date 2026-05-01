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

          string hashedPassword = passwordHasher.Hash(command.Password);

          var user = User.Create(
              new Email(command.Email),
              command.FirstName,
              command.LastName,
              hashedPassword);

             await userWriteRepository.AddAsync(user);

            user.Raise(new UserRegisteredDomainEvent(user.Id));

            await applicationDbContext.SaveChangesAsync(cancellationToken);

        return user.Id;
       
    }
}
