using Application.Abstractions.Messaging;

namespace Application.FlashcardCollection.Queries.GetFlashcardCollectionById;

public sealed record GetFlashcardCollectionByIdQuery(Guid FlashcardCollectionId) : IQuery<FlashcardCollectionDetailResponse>;
