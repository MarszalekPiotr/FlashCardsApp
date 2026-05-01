namespace Application.Abstractions.Authentication;

public interface ITokenProvider
{
    string Create(Guid userId, string email);
}
