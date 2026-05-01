using Application.Abstractions.Messaging;

namespace Application.FlashcardCollection.Queries.GetFlashcardById;

public sealed record GetFlashcardByIdQuery(Guid FlashcardId) : IQuery<FlashcardDetailResponse>;
