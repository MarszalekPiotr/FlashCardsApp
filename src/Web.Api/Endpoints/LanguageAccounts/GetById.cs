using Application.Abstractions.Messaging;
using Application.LanguageAccounts.Queries.GetLanguageAccountById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("language-accounts/{id:guid}", async (
            Guid id,
            IQueryHandler<GetLanguageAccountByIdQuery, LanguageAccountDetailResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetLanguageAccountByIdQuery(id);

            Result<LanguageAccountDetailResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
