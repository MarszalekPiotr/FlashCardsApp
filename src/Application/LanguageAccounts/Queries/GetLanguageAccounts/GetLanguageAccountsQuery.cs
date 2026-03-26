using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Queries.GetLanguageAccounts;

public sealed record GetLanguageAccountsQuery() : IQuery<List<LanguageAccountResponse>>;
