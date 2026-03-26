using System;
using System.Collections.Generic;
using System.Text;
using Application.Users.DTO;
using Application.Users.GetByEmail;
using Domain.Users;
using Domain.Users.ValueObjects;

namespace Application.Users;

public interface IUserWriteRepository
{
      Task<Guid> CreateUser (string email, string firstName, string lastName, string passwordHash);
      Task<bool> UserExists(string email);
     Task<User?>  GetUserByEmail(string email, CancellationToken cancellationToken);
}
