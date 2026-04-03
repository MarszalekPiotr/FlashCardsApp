using Application.Abstractions.Messaging;
using Application.LanguageAccounts.Commands.RenameFlashcardCollection;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts.FlashcardCollections;

internal sealed class Rename : IEndpoint
{
    public sealed record Request(string Name);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("language-accounts/{languageAccountId:guid}/collections/{collectionId:guid}", async (
            Guid languageAccountId,
            Guid collectionId,
            Request request,
            ICommandHandler<RenameFlashcardCollectionCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RenameFlashcardCollectionCommand(collectionId, request.Name);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
