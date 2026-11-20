using Microsoft.Extensions.DependencyInjection;
using ClinicManagement.Application.Patients.Services;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Appointments.Services;
using ClinicManagement.Application.AllergyTypes.Interfaces;
using ClinicManagement.Application.AllergyTypes.Services;
using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Clinics.Services;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClinicManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IAllergyTypeService, AllergyTypeService>();
        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<IProviderService, ProviderService>();

        return services;
    }
}