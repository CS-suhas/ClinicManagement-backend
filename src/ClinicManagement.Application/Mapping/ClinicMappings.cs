using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Clinics.Mapping;

public static class ClinicMappings
{
    public static Clinic ToEntity(this ClinicRequest request) =>
        Clinic.Create(request.Id, request.Name, request.Address, request.PhoneNumber);

    public static ClinicResponse ToResponse(this Clinic clinic) =>
        new(clinic.Id, clinic.Name, clinic.Address, clinic.PhoneNumber);

    public static ClinicListItemResponse ToListItem(this Clinic clinic, int providerCount) =>
        new(clinic.Id, clinic.Name, clinic.Address, clinic.PhoneNumber, providerCount);

    public static void ApplyTo(this ClinicRequest request, Clinic clinic)
    {
        clinic.SetName(request.Name);
        clinic.SetAddress(request.Address);
        clinic.SetPhoneNumber(request.PhoneNumber);
    }
}
