using ClinicManagement.Infrastructure;
using ClinicManagement.Application;
using ClinicManagement.Api.Endpoints;
using ClinicManagement.Application.Common.Settings;
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

