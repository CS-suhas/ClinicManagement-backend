using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Mapping;

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

    public static PatientListItemResponse ToListItem(
        this Patient patient,
        IReadOnlyDictionary<Guid, PatientAllergySummary> allergySummaries)
    {
        var summary = allergySummaries.GetValueOrDefault(patient.Id) ?? new PatientAllergySummary(0, null);
        return new PatientListItemResponse(
            patient.Id,
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.PhoneNumber,
            patient.Email,
            summary.AllergyCount,
            summary.HighestSeverity);
    }

    public static void ApplyTo(this PatientRequest request, Patient patient)
    {
        patient.SetFirstName(request.FirstName);
        patient.SetLastName(request.LastName);
        patient.SetDateOfBirth(request.DateOfBirth);
        patient.SetPhoneNumber(request.PhoneNumber);
        patient.SetEmail(request.Email);
    }
}
