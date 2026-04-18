using System;
using System.Collections.Generic;
using System.Text;
using Application.Shared.DTO;

namespace Application.Shared;

public interface ILanguageReadRepository
{
    Task<IReadOnlyCollection<LanguageDetailReadModel>> GetActiveLanguagesAsync();

}
