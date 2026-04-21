using System;
using System.Collections.Generic;
using System.Text;

namespace SharedKernel;

public static  class AuthorizationError
{
  
        public static Error Forbidden() => Error.Failure(
         "Forbidden",
         $"No access to the resource");
    
}
