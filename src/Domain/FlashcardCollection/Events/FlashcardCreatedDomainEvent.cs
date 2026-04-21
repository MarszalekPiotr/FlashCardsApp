using System;
using System.Collections.Generic;
using System.Text;
using SharedKernel;

namespace Domain.FlashcardCollection.Events;

public sealed record FlashcardCreatedDomainEvent(Guid FlashcardId) : IDomainEvent;
