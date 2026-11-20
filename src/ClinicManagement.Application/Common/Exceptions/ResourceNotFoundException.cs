namespace ClinicManagement.Application.Common.Exceptions;

public sealed class ResourceNotFoundException(string resourceName, Guid resourceId)
    : Exception($"{resourceName} '{resourceId}' was not found.");
