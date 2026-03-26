using Application.Abstractions.Messaging;
using Application.LanguageAccounts.Commands.UpdateProficiencyLevel;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts;

internal sealed class UpdateProficiencyLevel : IEndpoint
{
    public sealed record Request(int ProficiencyLevel);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("language-accounts/{id:guid}/proficiency-level", async (
            Guid id,
            Request request,
            ICommandHandler<UpdateProficiencyLevelCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateProficiencyLevelCommand(id, request.ProficiencyLevel);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
