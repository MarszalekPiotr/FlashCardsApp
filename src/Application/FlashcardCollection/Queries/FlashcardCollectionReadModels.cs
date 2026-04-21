namespace Application.FlashcardCollection.Queries;

public class FlashcardCollectionListReadModel
{
    public Guid Id { get; set; }
    public Guid LanguageAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class FlashcardCollectionDetailReadModel
{
    public Guid Id { get; set; }
    public Guid LanguageAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public List<FlashcardListReadModel> Flashcards { get; set; } = [];
}

public class FlashcardListReadModel
{
    public Guid Id { get; set; }
    public string SentenceWithBlanks { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<string> Synonyms { get; set; } = [];
}

public class FlashcardDetailReadModel
{
    public Guid Id { get; set; }
    public Guid FlashcardCollectionId { get; set; }
    public Guid UserId { get; set; }
    public string SentenceWithBlanks { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<string> Synonyms { get; set; } = [];
}

public class DueFlashcardReadModel
{
    public Guid Id { get; set; }
    public string SentenceWithBlanks { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<string> Synonyms { get; set; } = [];
    public DateTime? NextReviewDate { get; set; }
}
