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
                Id,
                UserId,
                JSON_VALUE(Language, '$.Code') AS LanguageCode,
                JSON_VALUE(Language, '$.FullName') AS LanguageFullName,
                ProficiencyLevel
            FROM dbo.LanguageAccounts
            WHERE UserId = @UserId";

        IEnumerable<LanguageAccountListReadModel> result =
            await _dbConnection.QueryAsync<LanguageAccountListReadModel>(sql, new { UserId = userId });

        return result.ToList();
    }

    public async Task<LanguageAccountDetailReadModel?> GetByIdAsync(Guid id)
    {
        string sql = @"
            SELECT
                Id,
                UserId,
                JSON_VALUE(Language, '$.Code') AS LanguageCode,
                JSON_VALUE(Language, '$.FullName') AS LanguageFullName,
                ProficiencyLevel
            FROM dbo.LanguageAccounts
            WHERE Id = @Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<LanguageAccountDetailReadModel>(sql, new { Id = id });
    }
}
