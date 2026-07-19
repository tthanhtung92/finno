using Finmy.Budgeting.Application.Envelopes;
using Finmy.Budgeting.Application.Envelopes.Dto;
using Finmy.Modularity;
using Finmy.Modularity.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Finmy.Budgeting.Api.Endpoints;

public sealed class EnvelopeEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/envelopes", async (CreateEnvelopeRequest req, EnvelopesService svc, CancellationToken cancellationToken) =>
            {
                var result = await svc.CreateAsync(req, cancellationToken);
                return result.Match(id => Results.Created($"/envelopes/{id}", new { id }));
            })
            .AddEndpointFilter<ValidationFilter<CreateEnvelopeRequest>>();

        endpoints
            .MapGet("/envelopes/{id:guid}", async (Guid id, EnvelopesService svc, CancellationToken cancellationToken) =>
            {
                var result = await svc.GetByIdAsync(id, cancellationToken);
                return result.Match(Results.Ok);
            });
    }
}
