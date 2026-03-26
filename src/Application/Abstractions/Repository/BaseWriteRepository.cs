using System;
using System.Collections.Generic;
using System.Text;
using Application.Abstractions.Data;

namespace Application.Abstractions.Repository;

public  class BaseWriteRepository
{    
    protected readonly IApplicationDbContext _applicationDbContext;
    public BaseWriteRepository(IApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }
}
