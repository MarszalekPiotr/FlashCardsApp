using Application.Abstractions.Messaging;
using Application.LanguageAccounts.Commands.AddFlashcardReview;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts.FlashcardCollections.Flashcards;

internal sealed class AddFlashcardReview : IEndpoint
{
    public sealed record Request(int ReviewResult);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "language-accounts/{languageAccountId:guid}/collections/{collectionId:guid}/flashcards/{flashcardId:guid}/reviews",
            async (
                Guid languageAccountId,
                Guid collectionId,
                Guid flashcardId,
                Request request,
                ICommandHandler<AddFlashcardReviewCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new AddFlashcardReviewCommand(flashcardId, request.ReviewResult);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
