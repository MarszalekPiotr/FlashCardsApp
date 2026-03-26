using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.LanguageAccount.ValueObjects;

public record ReviewResult
{
    public Enums.ReviewResult Value { get; }

    public ReviewResult(Enums.ReviewResult value)
    {
        Value = value;
    }


}
