namespace Application.Users;

public sealed record UserAuthReadModel(Guid Id, string Email, string PasswordHash);
