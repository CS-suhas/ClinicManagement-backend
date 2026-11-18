using ClinicManagement.Infrastructure;
using ClinicManagement.Application;
using ClinicManagement.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPatientEndpoints();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

