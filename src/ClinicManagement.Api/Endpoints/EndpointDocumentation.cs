namespace ClinicManagement.Api.Endpoints;

/// <summary>Shared OpenAPI response metadata for the endpoint groups.</summary>
internal static class EndpointDocumentation
{
    /// <summary>
    /// Documents the empty-bodied 401 (no or invalid token) and 403 (missing permission) that the authorization
    /// middleware returns before the endpoint runs.
    /// </summary>
    public static RouteHandlerBuilder ProducesAuthFailures(this RouteHandlerBuilder builder) =>
        builder
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

    /// <summary>Documents a 201 whose body is <c>{ "id": "..." }</c> and whose Location header points at the new resource.</summary>
    public static RouteHandlerBuilder ProducesCreated(this RouteHandlerBuilder builder) =>
        builder.Produces<CreatedResponse>(StatusCodes.Status201Created);
}

/// <summary>The body of a 201 Created response: the id of the new resource.</summary>
public sealed record CreatedResponse(Guid Id);
