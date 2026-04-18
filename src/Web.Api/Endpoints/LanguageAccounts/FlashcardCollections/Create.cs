using Application.Abstractions.Messaging;
using Application.FlashcardCollection.Commands.CreateFlashcardCollection;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts.FlashcardCollections;

internal sealed class Create : IEndpoint
{
    public sealed record Request(string Name);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("language-accounts/{languageAccountId:guid}/collections", async (
            Guid languageAccountId,
            Request request,
            ICommandHandler<CreateFlashcardCollectionCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateFlashcardCollectionCommand(languageAccountId, request.Name);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
