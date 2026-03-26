using Application.Abstractions.Messaging;
using Application.LanguageAccounts.Commands.DeleteLanguageAccount;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("language-accounts/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteLanguageAccountCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteLanguageAccountCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
