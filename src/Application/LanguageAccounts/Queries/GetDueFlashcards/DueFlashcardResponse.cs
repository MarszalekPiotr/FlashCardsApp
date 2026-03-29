namespace Application.LanguageAccounts.Queries.GetDueFlashcards;

public sealed class DueFlashcardResponse
{
    public Guid Id { get; init; }
    public string SentenceWithBlanks { get; init; } = string.Empty;
    public string Translation { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public List<string> Synonyms { get; init; } = [];
    public DateTime? NextReviewDate { get; init; }
}
