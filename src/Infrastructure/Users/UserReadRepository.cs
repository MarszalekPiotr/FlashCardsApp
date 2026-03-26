using System.Data;
using Application.Users;
using Application.Users.DTO;
using Dapper;
using Domain.Users;
using Microsoft.Identity.Client;

namespace Infrastructure.Users;

public class UserReadRepository : IUserReadRepository
{
    private readonly IDbConnection _dbConnection;

    public UserReadRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<UserReadModel> GetByEmailAsync(string email)
    {
        string sql = @"
            SELECT TOP 1 Id, Email, FirstName, LastName
            FROM Users
            WHERE Email = @Email";


        IEnumerable<UserReadModel> users = await _dbConnection.QueryAsync<UserReadModel>(
            sql,
            new { Email = email }
        );

        return users.FirstOrDefault();
    }

    public async Task<UserReadModel> GetById(Guid userId)
    {
        string sql = @"
            SELECT TOP 1 Id, Email, FirstName, LastName
            FROM Users
            WHERE Id = @UserId";


        IEnumerable<UserReadModel> users = await _dbConnection.QueryAsync<UserReadModel>(
            sql,
            new { UserId = userId }
        );

        return users.FirstOrDefault();
    }
}
