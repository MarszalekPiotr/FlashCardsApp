using Application.Abstractions.Data;
using Application.Abstractions.Repository;
using Application.Users;
using Domain.Users;
using Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Users;

public class UserWriteRepository : BaseWriteRepository, IUserWriteRepository
{
    public UserWriteRepository(IApplicationDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public async Task AddAsync(User user)
    {
        await _applicationDbContext.Users.AddAsync(user);
    }

    public async Task<bool> UserExists(string email, CancellationToken cancellationToken)
    {
        Result<Email> emailResult = Email.Create(email);
        if (emailResult.IsFailure)
        {
            // An invalid email can never match a stored user — no need to query the DB.
            return false;
        }

        return await _applicationDbContext.Users
            .AnyAsync(u => u.Email == emailResult.Value, cancellationToken);
    }
}
