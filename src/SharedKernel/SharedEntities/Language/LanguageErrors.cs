using System;
using System.Collections.Generic;
using System.Text;

namespace SharedKernel.SharedEntities.Language;

public static  class LanguageErrors 
{
    public static Error CodeNotAvailable(string code) => Error.Failure(
     "Language Code not available",
     $"The language code '{code}' is not valid.");
}
