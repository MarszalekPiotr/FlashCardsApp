using Application.Abstractions.Messaging;
using Application.LanguageAccounts.Queries.GetLanguageAccounts;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LanguageAccounts;

internal sealed class GetAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("language-accounts", async (
            IQueryHandler<GetLanguageAccountsQuery, List<LanguageAccountResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetLanguageAccountsQuery();

            Result<List<LanguageAccountResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.LanguageAccounts)
        .RequireAuthorization();
    }
}
