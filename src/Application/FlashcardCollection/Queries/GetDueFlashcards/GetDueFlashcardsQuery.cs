using Application.Abstractions.Messaging;

namespace Application.FlashcardCollection.Queries.GetDueFlashcards;

public sealed record GetDueFlashcardsQuery(Guid CollectionId) : IQuery<List<DueFlashcardResponse>>;
