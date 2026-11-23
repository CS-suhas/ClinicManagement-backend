namespace ClinicManagement.Application.Repositories;

public interface IClinicAdminRepository
{
    Task<bool> IsAdminOfClinicAsync(string identitySubject, Guid clinicId);
    Task<bool> IsAnyAdminAsync(string identitySubject);
}
