using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Queries.GetLanguageAccountById;

public sealed record GetLanguageAccountByIdQuery(Guid LanguageAccountId) : IQuery<LanguageAccountDetailResponse>;
