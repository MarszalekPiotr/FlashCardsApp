namespace Application.LanguageAccounts.Queries.GetLanguageAccounts;

public sealed class LanguageAccountResponse
{
    public Guid Id { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
    public string LanguageFullName { get; init; } = string.Empty;
    public int ProficiencyLevel { get; init; }
}
