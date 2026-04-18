using System;
using System.Collections.Generic;
using System.Text;

namespace SharedKernel.SharedEntities.Language;

public class Language : Entity
{   
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public bool IsActive { get; set; }

}
