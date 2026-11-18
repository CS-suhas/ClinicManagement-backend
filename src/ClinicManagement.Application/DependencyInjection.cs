using Microsoft.Extensions.DependencyInjection;
using ClinicManagement.Application.Patients.Services;
using ClinicManagement.Application.Patients.Interfaces;

namespace ClinicManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPatientManagementService, PatientManagementService>();

        return services;
    }
}