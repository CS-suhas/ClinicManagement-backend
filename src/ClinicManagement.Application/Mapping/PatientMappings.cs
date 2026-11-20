using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Patients.Mapping;

public static class PatientMappings
{
    public static Patient ToEntity(this PatientRequest request) =>
        Patient.Create(
            request.Id,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Email,
            request.PhoneNumber);

    public static PatientResponse ToResponse(this Patient patient) =>
        new(
            patient.Id,
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.PhoneNumber,
            patient.Email);

    public static void ApplyTo(this PatientRequest request, Patient patient)
    {
        patient.SetFirstName(request.FirstName);
        patient.SetLastName(request.LastName);
        patient.SetDateOfBirth(request.DateOfBirth);
        patient.SetPhoneNumber(request.PhoneNumber);
        patient.SetEmail(request.Email);
    }
}
