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
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        bool userExists = await userWriteRepository.UserExists(command.Email);
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

            Guid userId = await userWriteRepository.AddAsync(user);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return userId;
       
    }
}
