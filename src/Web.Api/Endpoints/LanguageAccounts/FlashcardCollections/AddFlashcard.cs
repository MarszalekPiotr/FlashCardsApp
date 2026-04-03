using Application.Abstractions.Messaging;
using Application.LanguageAccounts.Commands.AddFlashcardToCollection;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts.FlashcardCollections;

internal sealed class AddFlashcard : IEndpoint
{
    public sealed record Request(
        string SentenceWithBlanks,
        string Translation,
        string Answer,
        IEnumerable<string> Synonyms);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("language-accounts/{languageAccountId:guid}/collections/{collectionId:guid}/flashcards", async (
            Guid languageAccountId,
            Guid collectionId,
            Request request,
            ICommandHandler<AddFlashcardToCollectionCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AddFlashcardToCollectionCommand(
                collectionId,
                request.SentenceWithBlanks,
                request.Translation,
                request.Answer,
                request.Synonyms);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
