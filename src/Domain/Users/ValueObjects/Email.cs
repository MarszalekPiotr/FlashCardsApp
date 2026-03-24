using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Domain.Users.ValueObjects;

public record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (!IsValidEmail(value))
        {
            throw new ArgumentException("Invalid email format.", nameof(value));
        }

        Value = value;

    }
    
    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email,
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
    public override string ToString() => Value;
}
