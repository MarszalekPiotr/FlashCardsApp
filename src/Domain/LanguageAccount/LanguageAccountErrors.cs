using SharedKernel;

namespace Domain.LanguageAccount;

public static class LanguageAccountErrors
{
    public static Error NotFound(Guid languageAccountId) => Error.NotFound(
        "LanguageAccounts.NotFound",
        $"The language account with Id = '{languageAccountId}' was not found.");
}
