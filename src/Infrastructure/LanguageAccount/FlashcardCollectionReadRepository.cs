using System.Data;
using System.Text.Json;
using Application.FlashcardCollection;
using Application.FlashcardCollection.Queries;
using Dapper;

namespace Infrastructure.LanguageAccount;

public class FlashcardCollectionReadRepository : IFlashcardCollectionReadRepository
{
    private readonly IDbConnection _dbConnection;

    public FlashcardCollectionReadRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<Guid?> GetLanguageAccountUserIdAsync(Guid languageAccountId)
    {
        string sql = @"
            SELECT TOP 1 UserId
            FROM dbo.LanguageAccounts
            WHERE Id = @LanguageAccountId";

        return await _dbConnection.QuerySingleOrDefaultAsync<Guid?>(sql, new { LanguageAccountId = languageAccountId });
    }

    public async Task<List<FlashcardCollectionListReadModel>> GetByLanguageAccountIdAsync(Guid languageAccountId)
    {
        string sql = @"
            SELECT Id, LanguageAccountId, Name
            FROM dbo.FlashcardCollections
            WHERE LanguageAccountId = @LanguageAccountId";

        IEnumerable<FlashcardCollectionListReadModel> result =
            await _dbConnection.QueryAsync<FlashcardCollectionListReadModel>(sql, new { LanguageAccountId = languageAccountId });

        return result.ToList();
    }

    public async Task<FlashcardCollectionDetailReadModel?> GetByIdAsync(Guid id)
    {
        string sql = @"
            SELECT fc.Id, fc.LanguageAccountId, fc.Name, la.UserId,
                   f.Id AS FlashcardId, f.SentenceWithBlanks, f.Translation, f.Answer, f.Synonyms
            FROM dbo.FlashcardCollections fc
            INNER JOIN dbo.LanguageAccounts la ON la.Id = fc.LanguageAccountId
            LEFT JOIN dbo.Flashcards f ON f.FlashcardCollectionId = fc.Id
            WHERE fc.Id = @Id";

        FlashcardCollectionDetailReadModel? collectionDetail = null;

        await _dbConnection.QueryAsync<FlashcardCollectionDetailReadModel, FlashcardRow, FlashcardCollectionDetailReadModel>(
            sql,
            (collection, flashcard) =>
            {
                collectionDetail ??= collection;

                if (flashcard is not null && flashcard.FlashcardId != Guid.Empty)
                {
                    collectionDetail.Flashcards.Add(new FlashcardListReadModel
                    {
                        Id = flashcard.FlashcardId,
                        SentenceWithBlanks = flashcard.SentenceWithBlanks,
                        Translation = flashcard.Translation,
                        Answer = flashcard.Answer,
                        Synonyms = JsonSerializer.Deserialize<List<string>>(flashcard.Synonyms ?? "[]") ?? []
                    });
                }

                return collectionDetail;
            },
            new { Id = id },
            splitOn: "FlashcardId");

        return collectionDetail;
    }

    private sealed class FlashcardRow
    {
        public Guid FlashcardId { get; set; } = Guid.Empty;
        public string SentenceWithBlanks { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string? Synonyms { get; set; }
    }

    public async Task<FlashcardDetailReadModel?> GetFlashcardByIdAsync(Guid flashcardId)
    {
        string sql = @"
            SELECT
                f.Id,
                f.FlashcardCollectionId,
                la.UserId,
                f.SentenceWithBlanks,
                f.Translation,
                f.Answer,
                f.Synonyms
            FROM dbo.Flashcards f
            INNER JOIN dbo.FlashcardCollections fc ON fc.Id = f.FlashcardCollectionId
            INNER JOIN dbo.LanguageAccounts la ON la.Id = fc.LanguageAccountId
            WHERE f.Id = @FlashcardId";

        FlashcardRawRow? raw =
            await _dbConnection.QuerySingleOrDefaultAsync<FlashcardRawRow>(sql, new { FlashcardId = flashcardId });

        if (raw is null)
        {
            return null;
        }

        return new FlashcardDetailReadModel
        {
            Id = raw.Id,
            FlashcardCollectionId = raw.FlashcardCollectionId,
            UserId = raw.UserId,
            SentenceWithBlanks = raw.SentenceWithBlanks,
            Translation = raw.Translation,
            Answer = raw.Answer,
            Synonyms = JsonSerializer.Deserialize<List<string>>(raw.Synonyms ?? "[]") ?? []
        };
    }

    private sealed class FlashcardRawRow
    {
        public Guid Id { get; set; }
        public Guid FlashcardCollectionId { get; set; }
        public Guid UserId { get; set; }
        public string SentenceWithBlanks { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string? Synonyms { get; set; }
    }

    public async Task<List<DueFlashcardReadModel>> GetDueFlashcardsAsync(Guid collectionId, Guid userId)
    {
        string sql = @"
            SELECT
                f.Id,
                f.SentenceWithBlanks,
                f.Translation,
                f.Answer,
                f.Synonyms,
                ss.NextReviewDate
            FROM dbo.Flashcards f
            INNER JOIN dbo.FlashcardCollections fc ON fc.Id = f.FlashcardCollectionId
            INNER JOIN dbo.LanguageAccounts la ON la.Id = fc.LanguageAccountId
            LEFT JOIN dbo.SrsStates ss ON ss.FlashcardId = f.Id
            WHERE f.FlashcardCollectionId = @CollectionId
              AND la.UserId = @UserId
              AND (ss.NextReviewDate IS NULL OR ss.NextReviewDate <= GETUTCDATE())";

        IEnumerable<DueFlashcardRawRow> rows = await _dbConnection.QueryAsync<DueFlashcardRawRow>(
            sql,
            new { CollectionId = collectionId, UserId = userId });

        return rows
            .Select(r => new DueFlashcardReadModel
            {
                Id = r.Id,
                SentenceWithBlanks = r.SentenceWithBlanks,
                Translation = r.Translation,
                Answer = r.Answer,
                Synonyms = JsonSerializer.Deserialize<List<string>>(r.Synonyms ?? "[]") ?? [],
                NextReviewDate = r.NextReviewDate
            })
            .ToList();
    }

    private sealed class DueFlashcardRawRow
    {
        public Guid Id { get; set; }
        public string SentenceWithBlanks { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string? Synonyms { get; set; }
        public DateTime? NextReviewDate { get; set; }
    }
}
