using ClinicManagement.Infrastructure;
using ClinicManagement.Application;
using ClinicManagement.Api.Endpoints;
using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddOptions<ClinicManagementSettings>()
    .BindConfiguration(ClinicManagementSettings.SectionName)
    .Validate(
        settings => settings.DefaultAppointmentDurationMinutes > 0,
        "Default appointment duration must be greater than zero.")
    .Validate(
        settings => settings.DefaultClinicEndTime > settings.DefaultClinicStartTime,
        "Default clinic end time must be after its start time.")
    .ValidateOnStart();
builder.Services.AddSingleton(
    serviceProvider => serviceProvider.GetRequiredService<IOptions<ClinicManagementSettings>>().Value);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    var clinicSettings = scope.ServiceProvider.GetRequiredService<ClinicManagementSettings>();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPatientEndpoints();
app.MapAppointmentEndpoints();
app.MapAllergyEndpoints();
app.MapAllergyTypeEndpoints();
app.MapClinicEndpoints();
app.MapProviderEndpoints();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

