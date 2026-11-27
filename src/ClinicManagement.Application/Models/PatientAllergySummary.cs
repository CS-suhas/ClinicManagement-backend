using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

public sealed record PatientAllergySummary(int AllergyCount, AllergySeverity? HighestSeverity);
