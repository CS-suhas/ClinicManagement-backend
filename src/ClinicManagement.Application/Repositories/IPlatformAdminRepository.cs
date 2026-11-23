namespace ClinicManagement.Application.Repositories;

public interface IPlatformAdminRepository
{
    Task<bool> IsAdminAsync(string identitySubject);
}
