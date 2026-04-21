using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Application.LanguageAccounts.DTO;
using Application.Shared;
using Application.Shared.DTO;
using Dapper;
using Domain.Users;

namespace Infrastructure.Shared.Repositories;

public class LanguageReadRepository : ILanguageReadRepository
{
    private readonly IDbConnection _dbConnection;

    public LanguageReadRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IReadOnlyCollection<LanguageDetailReadModel>> GetActiveLanguagesAsync()
    {
          var sql = @"SELECT Id, Name, Code FROM Languages WHERE IsActive = 1";


        IEnumerable<LanguageDetailReadModel> result =
            await _dbConnection.QueryAsync<LanguageDetailReadModel>(sql);

        return result.AsList();
    }
}
