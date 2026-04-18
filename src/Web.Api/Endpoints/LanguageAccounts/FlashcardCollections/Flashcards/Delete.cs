using Application.Abstractions.Messaging;
using Application.FlashcardCollection.Commands.DeleteFlashcard;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts.FlashcardCollections.Flashcards;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "language-accounts/{languageAccountId:guid}/collections/{collectionId:guid}/flashcards/{flashcardId:guid}",
            async (
                Guid languageAccountId,
                Guid collectionId,
                Guid flashcardId,
                ICommandHandler<DeleteFlashcardCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteFlashcardCommand(collectionId,flashcardId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
