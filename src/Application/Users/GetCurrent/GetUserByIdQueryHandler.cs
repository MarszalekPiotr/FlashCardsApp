using System.Data;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Users.DTO;
using Application.Users.GetById;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetCurrent;

internal sealed class GetCurrentUserQueryHandler(IUserContext userContext,IUserReadRepository userReadRepository)
    : IQueryHandler<GetCurrentUserQuery, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(GetCurrentUserQuery   query, CancellationToken cancellationToken)
    {
        

       UserReadModel user = await userReadRepository.GetById(userContext.UserId);

        if (user is null)
        {
            return Result.Failure<UserResponse>(UserErrors.NotFound(userContext.UserId));
        }

        var userResponse = new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email.ToString()
        };     

        return userResponse;
    }
}
