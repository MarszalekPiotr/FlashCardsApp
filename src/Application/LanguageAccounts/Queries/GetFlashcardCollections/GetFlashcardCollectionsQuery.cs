using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Queries.GetFlashcardCollections;

public sealed record GetFlashcardCollectionsQuery(Guid LanguageAccountId) : IQuery<List<FlashcardCollectionResponse>>;
