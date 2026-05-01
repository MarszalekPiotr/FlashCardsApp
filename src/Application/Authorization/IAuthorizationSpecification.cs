using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Authorization;

public interface IAuthorizationSpecification<T>
{
    Task<bool> IsSatisfiedByAsync(T entity, Guid userId,  CancellationToken cancellationToken);
    
}
