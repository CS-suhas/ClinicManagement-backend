using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Interfaces;

public interface IMeService
{
    Task<MeResponse> GetCurrentUserAsync();
    Task<Guid> CreatePatientProfileAsync(PatientRequest request);
}
