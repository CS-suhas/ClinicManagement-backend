using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ClinicManagement.Api.Middleware;

internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails = exception switch
        {
            ResourceNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            },
            DomainValidationException validationException => CreateValidationProblem(
                validationException,
                httpContext.Request.Path),
            DomainConflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            },
            ForbiddenException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            },
            _ => CreateInternalProblem(httpContext, exception)
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problemDetails,
            problemDetails.GetType(),
            JsonOptions,
            cancellationToken);
        return true;
    }

    private static ValidationProblemDetails CreateValidationProblem(
        DomainValidationException exception,
        string? instance)
    {
        var errors = new Dictionary<string, string[]>
        {
            [exception.PropertyName ?? string.Empty] = [exception.Message]
        };

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Instance = instance
        };
    }

    private ProblemDetails CreateInternalProblem(HttpContext httpContext, Exception exception)
    {
        logger.LogError(
            exception,
            "Unhandled exception processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Detail = "The request could not be completed.",
            Instance = httpContext.Request.Path
        };
    }
}
