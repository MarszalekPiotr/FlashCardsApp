using SharedKernel;

namespace Domain.SRS.Events;

public sealed record FlashcardReviewedDomainEvent(Guid FlashCardReviewId, Guid FlashcardId, int ReviewResultValue) : IDomainEvent;
