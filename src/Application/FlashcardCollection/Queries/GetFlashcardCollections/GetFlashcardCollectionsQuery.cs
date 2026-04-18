using Application.Abstractions.Messaging;

namespace Application.FlashcardCollection.Queries.GetFlashcardCollections;

public sealed record GetFlashcardCollectionsQuery(Guid LanguageAccountId) : IQuery<List<FlashcardCollectionResponse>>;
