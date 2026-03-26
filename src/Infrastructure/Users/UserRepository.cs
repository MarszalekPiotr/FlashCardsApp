using Application.Abstractions.Data;
using Application.Abstractions.Repository;
using Application.Users;
using Domain.Users;
using Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Infrastructure.Users;

public class UserWriteRepository : BaseWriteRepository, IUserWriteRepository
{
    public UserWriteRepository(IApplicationDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public async Task<Guid> CreateUser(string email, string firstName, string lastName, string passwordHash)
    {
        var user = User.Create(new Email(email), firstName, lastName, passwordHash);
        await _applicationDbContext.Users.AddAsync(user);

        return user.Id;
    }

    public async Task<User?> GetUserByEmail(string email, CancellationToken cancellationToken)
    {
       var emailValueObject = new Email(email);
       return await _applicationDbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == emailValueObject, cancellationToken);
    }

    public async Task<bool> UserExists(string email)
    {
        var emailValueObject = new Email(email);
        return await _applicationDbContext.Users
            .AnyAsync(u => u.Email == emailValueObject);
    }
}
