using System.Data;
using Application.LanguageAccounts;
using Application.LanguageAccounts.DTO;
using Dapper;

namespace Infrastructure.LanguageAccount;

public class LanguageAccountReadRepository : ILanguageAccountReadRepository
{
    private readonly IDbConnection _dbConnection;

    public LanguageAccountReadRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<List<LanguageAccountListReadModel>> GetByUserIdAsync(Guid userId)
    {
        string sql = @"
            SELECT
                la.Name,
                la.Id,
                la.UserId,
                l.Code AS LanguageCode,
                l.Name AS LanguageFullName,
                la.ProficiencyLevel
            FROM dbo.LanguageAccounts la
            INNER JOIN dbo.Languages l
            ON la.LanguageId = l.Id
            WHERE la.UserId = @UserId
            AND la.IsDeleted = 0";

        IEnumerable<LanguageAccountListReadModel> result =
            await _dbConnection.QueryAsync<LanguageAccountListReadModel>(sql, new { UserId = userId });

        return result.ToList();
    }

    public async Task<LanguageAccountDetailReadModel?> GetByIdAsync(Guid id)
    {
        string sql = @"
            SELECT
                la.Id,
                la.UserId,
                l.Code AS LanguageCode,
                l.Name AS LanguageFullName,
                la.ProficiencyLevel
            FROM dbo.LanguageAccounts la
            INNER JOIN dbo.Languages l
            ON la.LanguageId = l.Id
            WHERE la.Id = @Id
              AND la.IsDeleted = 0";

        return await _dbConnection.QuerySingleOrDefaultAsync<LanguageAccountDetailReadModel>(sql, new { Id = id });
    }
}
