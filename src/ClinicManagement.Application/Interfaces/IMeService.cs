using ClinicManagement.Application.Models;
using ClinicManagement.Application.Patients.Models;

namespace ClinicManagement.Application.Interfaces;

public interface IMeService
{
    Task<MeResponse> GetCurrentUserAsync();
    Task<Guid> CreatePatientProfileAsync(PatientRequest request);
}
