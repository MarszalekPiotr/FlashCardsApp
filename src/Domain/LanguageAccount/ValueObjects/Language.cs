using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.LanguageAccount.ValueObjects;

public record  Language
{   
    public string Code { get; init; }
    public string FullName { get; init; }
    public Language(string code, string fullName)
    {
        Code = code;
        FullName = fullName;
    }

    public static Language English => new Language("en", "English");
    public static Language Polish => new Language("pl", "Polish");
    public static Language Spanish => new Language("es", "Spanish");
    public static Language French => new Language("fr", "French");
    public static Language German => new Language("de", "German");

    public static IEnumerable<Language> GetSupportedLanguages()
    {
        return new List<Language>
        {
            English,
            Polish,
            Spanish,
            French,
            German
        };
    }
}
