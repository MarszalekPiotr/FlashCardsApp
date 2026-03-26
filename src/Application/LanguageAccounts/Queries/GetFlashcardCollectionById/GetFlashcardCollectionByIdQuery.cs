using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Queries.GetFlashcardCollectionById;

public sealed record GetFlashcardCollectionByIdQuery(Guid FlashcardCollectionId) : IQuery<FlashcardCollectionDetailResponse>;
