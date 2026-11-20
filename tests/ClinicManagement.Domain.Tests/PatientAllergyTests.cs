using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Tests;

public class PatientAllergyTests
{
    [Fact]
    public void AddAllergy_RejectsAnExistingAllergyType()
    {
        var patient = CreatePatient();
        var allergyTypeId = Guid.NewGuid();
        var allergyType = AllergyType.Create(allergyTypeId, "Peanuts");

        patient.AddAllergy(allergyType, AllergySeverity.MILD, null);

        var duplicateType = AllergyType.Create(allergyTypeId, "Peanuts");

        Assert.Throws<DuplicatePatientAllergyException>(
            () => patient.AddAllergy(duplicateType, AllergySeverity.SEVERE, "Duplicate entry"));
        Assert.Single(patient.Allergies);
    }

    [Fact]
    public void AddAllergy_AllowsDifferentAllergyTypes()
    {
        var patient = CreatePatient();

        patient.AddAllergy(
            AllergyType.Create(Guid.NewGuid(), "Peanuts"),
            AllergySeverity.MILD,
            null);
        patient.AddAllergy(
            AllergyType.Create(Guid.NewGuid(), "Dairy"),
            AllergySeverity.MODERATE,
            null);

        Assert.Equal(2, patient.Allergies.Count);
    }

    private static Patient CreatePatient() =>
        Patient.Create(
            Guid.NewGuid(),
            "Test",
            "Patient",
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            "test@example.com",
            "555-0100");
}
