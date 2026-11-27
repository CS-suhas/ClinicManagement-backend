namespace ClinicManagement.Api.Endpoints;

internal static class EndpointDocumentation
{
    public static RouteHandlerBuilder ProducesAuthFailures(this RouteHandlerBuilder builder) =>
        builder
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

    public static RouteHandlerBuilder ProducesCreated(this RouteHandlerBuilder builder) =>
        builder.Produces<CreatedResponse>(StatusCodes.Status201Created);
}

public sealed record CreatedResponse(Guid Id);
