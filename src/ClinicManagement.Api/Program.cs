using ClinicManagement.Infrastructure;
using ClinicManagement.Application;
using ClinicManagement.Api.Endpoints;
using ClinicManagement.Api.Middleware;
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
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
var corsAllowedOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"]?
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPatientEndpoints();
app.MapAppointmentEndpoints();
app.MapAllergyTypeEndpoints();
app.MapClinicEndpoints();
app.MapProviderEndpoints();
app.MapReferenceDataEndpoints();
app.MapStatusEndpoints();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

