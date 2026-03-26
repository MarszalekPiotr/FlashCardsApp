using Application.Abstractions.Messaging;
using Application.LanguageAccounts.Commands.CreateLanguageAccount;
using Microsoft.AspNetCore.Authorization;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts;

internal sealed class Create : IEndpoint
{
    public sealed record Request(string LanguageCode, int ProficiencyLevel);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("language-accounts", async (
            Request request,
            ICommandHandler<CreateLanguageAccountCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateLanguageAccountCommand(request.LanguageCode, request.ProficiencyLevel);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
