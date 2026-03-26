using Application.Abstractions.Messaging;
using Application.LanguageAccounts.Commands.UpdateFlashcard;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts.FlashcardCollections.Flashcards;

internal sealed class Update : IEndpoint
{
    public sealed record Request(
        string SentenceWithBlanks,
        string Translation,
        string Answer,
        List<string> Synonyms);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "language-accounts/{languageAccountId:guid}/collections/{collectionId:guid}/flashcards/{flashcardId:guid}",
            async (
                Guid flashcardId,
                Request request,
                ICommandHandler<UpdateFlashcardCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateFlashcardCommand(
                    flashcardId,
                    request.SentenceWithBlanks,
                    request.Translation,
                    request.Answer,
                    request.Synonyms);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
