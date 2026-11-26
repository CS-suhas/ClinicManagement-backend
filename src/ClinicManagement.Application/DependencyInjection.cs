using Microsoft.Extensions.DependencyInjection;
using ClinicManagement.Application.Services;
using ClinicManagement.Application.Interfaces;
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
        services.AddScoped<IStatusService, StatusService>();
        services.AddScoped<IMeService, MeService>();
        services.AddScoped<IMeDashboardService, MeDashboardService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IUserRoleService, UserRoleService>();

        return services;
    }
}