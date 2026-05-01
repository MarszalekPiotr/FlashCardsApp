namespace Application.FlashcardCollection.Queries.GetFlashcardById;

public sealed class FlashcardDetailResponse
{
    public Guid Id { get; init; }
    public Guid FlashcardCollectionId { get; init; }
    public string SentenceWithBlanks { get; init; } = string.Empty;
    public string Translation { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public List<string> Synonyms { get; init; } = [];
}
