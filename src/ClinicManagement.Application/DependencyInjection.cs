using Microsoft.Extensions.DependencyInjection;
using ClinicManagement.Application.Patients.Services;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Appointments.Services;

namespace ClinicManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPatientManagementService, PatientManagementService>();
        services.AddScoped<IAppointmentManagementService, AppointmentManagementService>();

        return services;
    }
}