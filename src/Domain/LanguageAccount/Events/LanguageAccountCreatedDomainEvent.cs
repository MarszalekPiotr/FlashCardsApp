using System;
using System.Collections.Generic;
using System.Text;
using SharedKernel;

namespace Domain.LanguageAccount.Events;

public sealed record LanguageAccountCreatedDomainEvent(Guid LanguageAccountId) : IDomainEvent;
