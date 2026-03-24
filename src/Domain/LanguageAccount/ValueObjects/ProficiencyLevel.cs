using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.LanguageAccount.ValueObjects;

public record ProficiencyLevel
{
    public Enums.ProficiencyLevel Value { get; }

    public ProficiencyLevel(Enums.ProficiencyLevel value)
    {
        Value = value;
    }


}
