using Microsoft.Extensions.DependencyInjection;
using ClinicManagement.Application.Patients.Services;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Appointments.Services;
using ClinicManagement.Application.Allergies.Interfaces;
using ClinicManagement.Application.Allergies.Services;
using ClinicManagement.Application.AllergyTypes.Interfaces;
using ClinicManagement.Application.AllergyTypes.Services;
using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Clinics.Services;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Services;

namespace ClinicManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPatientManagementService, PatientManagementService>();
        services.AddScoped<IAppointmentManagementService, AppointmentManagementService>();
        services.AddScoped<IAllergyManagementService, AllergyManagementService>();
        services.AddScoped<IAllergyTypeManagementService, AllergyTypeManagementService>();
        services.AddScoped<IClinicManagementService, ClinicManagementService>();
        services.AddScoped<IProviderManagementService, ProviderManagementService>();

        return services;
    }
}