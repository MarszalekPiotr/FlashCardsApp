using SharedKernel;

namespace Domain.LanguageAccount.Events;

public sealed record FlashcardReviewedDomainEvent(Guid FlashCardReviewId, Guid FlashcardId, int ReviewResultValue) : IDomainEvent;
