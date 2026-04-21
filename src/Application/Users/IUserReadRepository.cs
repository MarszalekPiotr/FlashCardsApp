using Application.Users.DTO;

namespace Application.Users;

public interface IUserReadRepository
{
    Task<UserReadModel> GetById(Guid userId);
    Task<UserReadModel> GetByEmailAsync(string email);
    Task<UserAuthReadModel?> GetByEmailForAuthAsync(string email, CancellationToken cancellationToken);
}
