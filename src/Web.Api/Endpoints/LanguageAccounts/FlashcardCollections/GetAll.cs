using Application.Abstractions.Messaging;
using Application.FlashcardCollection.Queries.GetFlashcardCollections;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts.FlashcardCollections;

internal sealed class GetAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("language-accounts/{languageAccountId:guid}/collections", async (
            Guid languageAccountId,
            IQueryHandler<GetFlashcardCollectionsQuery, List<FlashcardCollectionResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetFlashcardCollectionsQuery(languageAccountId);

            Result<List<FlashcardCollectionResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
