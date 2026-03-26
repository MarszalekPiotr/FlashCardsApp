using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Queries.GetFlashcardById;

public sealed record GetFlashcardByIdQuery(Guid FlashcardId) : IQuery<FlashcardDetailResponse>;
