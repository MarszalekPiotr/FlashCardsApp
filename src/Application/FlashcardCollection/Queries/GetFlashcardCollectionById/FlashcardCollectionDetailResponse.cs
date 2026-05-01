namespace Application.FlashcardCollection.Queries.GetFlashcardCollectionById;

public sealed class FlashcardResponse
{
    public Guid Id { get; init; }
    public string SentenceWithBlanks { get; init; } = string.Empty;
    public string Translation { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public IEnumerable<string> Synonyms { get; init; } = [];
}

public sealed class FlashcardCollectionDetailResponse
{
    public Guid Id { get; init; }
    public Guid LanguageAccountId { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<FlashcardResponse> Flashcards { get; init; } = [];
}
