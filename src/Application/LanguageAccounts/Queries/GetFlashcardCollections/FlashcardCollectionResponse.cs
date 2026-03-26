namespace Application.LanguageAccounts.Queries.GetFlashcardCollections;

public sealed class FlashcardCollectionResponse
{
    public Guid Id { get; init; }
    public Guid LanguageAccountId { get; init; }
    public string Name { get; init; } = string.Empty;
}
