using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain.LanguageAccount.ValueObjects;

public record Synonyms
{
    public IEnumerable<string> Value { get; }

    public Synonyms(IEnumerable<string> value)
    {
        List<string> synonymList = value?.ToList() ?? throw new ArgumentNullException(nameof(value));

        if (synonymList.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Synonyms cannot contain null or whitespace values.", nameof(value));
        }

        if (!AreUniqueSynonyms(synonymList))
        {
            throw new ArgumentException("Synonyms must be unique (case-insensitive).", nameof(value));
        }

        Value = synonymList;
    }

    private bool AreUniqueSynonyms(List<string> synonyms)
    {
        int distinctCount = synonyms
            .Select(s => s.Trim().ToUpperInvariant())
            .Distinct()
            .Count();

        return distinctCount == synonyms.Count;
    }

}
