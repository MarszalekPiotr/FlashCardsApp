using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Queries.GetDueFlashcards;

public sealed record GetDueFlashcardsQuery(Guid CollectionId) : IQuery<List<DueFlashcardResponse>>;
