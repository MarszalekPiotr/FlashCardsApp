using Application.Abstractions.Messaging;
using Application.LanguageAccounts.Queries.GetFlashcardCollectionById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts.FlashcardCollections;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("language-accounts/{languageAccountId:guid}/collections/{collectionId:guid}", async (
            Guid collectionId,
            IQueryHandler<GetFlashcardCollectionByIdQuery, FlashcardCollectionDetailResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetFlashcardCollectionByIdQuery(collectionId);

            Result<FlashcardCollectionDetailResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
