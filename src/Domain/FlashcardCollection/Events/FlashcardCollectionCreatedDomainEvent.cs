using SharedKernel;

namespace Domain.FlashcardCollection.Events;

public sealed record FlashcardCollectionCreatedDomainEvent(Guid FlashcardCollectionId) : IDomainEvent;
