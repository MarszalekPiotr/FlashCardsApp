using SharedKernel;

namespace Domain.LanguageAccount.Events;

public sealed record FlashcardCollectionCreatedDomainEvent(Guid FlashcardCollectionId) : IDomainEvent;
