namespace Application.LanguageAccounts.DTO;

public class LanguageAccountListReadModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageFullName { get; set; } = string.Empty;
    public int ProficiencyLevel { get; set; }
}

public class LanguageAccountDetailReadModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageFullName { get; set; } = string.Empty;
    public int ProficiencyLevel { get; set; }
}
