using Application.Abstractions.Messaging;
using Application.LanguageAccounts.Queries.GetDueFlashcards;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts.FlashcardCollections.Flashcards;

internal sealed class GetDue : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "language-accounts/{languageAccountId:guid}/collections/{collectionId:guid}/flashcards/due",
            async (
                Guid collectionId,
                IQueryHandler<GetDueFlashcardsQuery, List<DueFlashcardResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetDueFlashcardsQuery(collectionId);

                Result<List<DueFlashcardResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
