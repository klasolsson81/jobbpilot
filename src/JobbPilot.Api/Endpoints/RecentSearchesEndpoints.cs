using JobbPilot.Application.RecentJobSearches.Commands.DeleteRecentSearch;
using JobbPilot.Application.RecentJobSearches.Queries.ListRecentSearches;
using Mediator;

namespace JobbPilot.Api.Endpoints;

/// <summary>
/// ADR 0060 — RecentJobSearches (auto-fångade sökningar per JobSeeker).
/// Route-prefix <c>/api/v1/me/recent-searches</c> följer "my data"-konvention
/// från <see cref="MeEndpoints"/>. Auto-capture sker via pipeline-behavior på
/// ListJobAds-flöden; dessa endpoints exponerar bara läs + radera.
/// </summary>
public static class RecentSearchesEndpoints
{
    public static void MapRecentSearchesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/me/recent-searches")
            .WithTags("RecentSearches")
            .RequireAuthorization();

        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListRecentSearchesQuery(), ct);
            return Results.Ok(result);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteRecentSearchCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    detail: result.Error.Message,
                    title: result.Error.Code,
                    statusCode: result.Error.Code.EndsWith("NotFound", StringComparison.Ordinal) ? 404 : 400);
        });
    }
}
