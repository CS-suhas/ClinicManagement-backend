namespace ClinicManagement.Application.Common.Exceptions;

public sealed class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string resourceName, Guid resourceId)
        : base($"{resourceName} '{resourceId}' was not found.")
    {
    }

    public ResourceNotFoundException(string message) : base(message)
    {
    }
}
