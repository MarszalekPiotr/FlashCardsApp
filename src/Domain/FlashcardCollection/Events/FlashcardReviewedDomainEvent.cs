using Domain.FlashcardCollection.Enums;
using SharedKernel;

namespace Domain.FlashcardCollection.Events;

public sealed record FlashcardReviewedDomainEvent(Guid FlashCardReviewId,Guid FlashcardCollectionId, Guid FlashcardId, DateTime ReviewDate, ReviewResult ReviewResult) : IDomainEvent;
