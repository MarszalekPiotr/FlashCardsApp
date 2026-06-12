using System;
using System.Collections.Generic;
using System.Text;
using Domain.Users.ValueObjects;

namespace Application.Users.DTO;

public class UserReadModel
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
