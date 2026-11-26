using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ClinicManagement.Infrastructure.Data;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Infrastructure.Repositories;

namespace ClinicManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");

        OracleDapperTypeHandlers.Register();
        services.AddSingleton<IDbConnectionFactory>(new OracleConnectionFactory(connectionString));
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IAllergyTypeRepository, AllergyTypeRepository>();
        services.AddScoped<IStatusRepository, StatusRepository>();
        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<IClinicAdminRepository, ClinicAdminRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();

        return services;
    }
}