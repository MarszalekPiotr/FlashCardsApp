using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Shared.DTO;

public class LanguageDetailReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } 
    public string Code { get; set; }
    public bool IsActive { get; set; }  
}
