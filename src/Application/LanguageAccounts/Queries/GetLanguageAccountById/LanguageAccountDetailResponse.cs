namespace Application.LanguageAccounts.Queries.GetLanguageAccountById;

public sealed class LanguageAccountDetailResponse
{
    public Guid Id { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
    public string LanguageFullName { get; init; } = string.Empty;
    public int ProficiencyLevel { get; init; }
}
