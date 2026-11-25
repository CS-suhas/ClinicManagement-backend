using ClinicManagement.Api.Security;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this WebApplication app)
    {
        app.MapGet("/api/search", async (string? q, string? limit, ISearchService service) =>
                Results.Ok(await service.SearchAsync(q, limit)))
            .WithTags(ApiTags.Search)
            .WithName("GlobalSearch")
            .WithSummary("Search patients, providers and clinics")
            .WithDescription("Signed-in users. Searches by name, email, specialty and address, case-insensitively. q is required, trimmed, and at least 2 characters. limit is 1 to 20 (default 5) and caps each list. Patients are limited to those the caller treats as theirs: all for administrators; for a verified doctor, patients with an appointment with them; for a clinic administrator, patients with an appointment at a clinic they administer; empty for anyone else. Providers are verified only unless the caller is an administrator. Clinics are visible to all. Each list is ordered by name. Returns 400 for an invalid q or limit.")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<SearchResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}
