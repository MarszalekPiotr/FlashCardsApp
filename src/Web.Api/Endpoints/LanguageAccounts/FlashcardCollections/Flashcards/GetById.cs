using Application.Abstractions.Messaging;
using Application.FlashcardCollection.Queries.GetFlashcardById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts.FlashcardCollections.Flashcards;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "language-accounts/{languageAccountId:guid}/collections/{collectionId:guid}/flashcards/{flashcardId:guid}",
            async (
                Guid languageAccountId,
                Guid collectionId,
                Guid flashcardId,
                IQueryHandler<GetFlashcardByIdQuery, FlashcardDetailResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetFlashcardByIdQuery(flashcardId);

                Result<FlashcardDetailResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
