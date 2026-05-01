using Application.Abstractions.Messaging;
using Application.FlashcardCollection.Commands.DeleteFlashcardCollection;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts.FlashcardCollections;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("language-accounts/{languageAccountId:guid}/collections/{collectionId:guid}", async (
            Guid languageAccountId,
            Guid collectionId,
            ICommandHandler<DeleteFlashcardCollectionCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteFlashcardCollectionCommand(collectionId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
