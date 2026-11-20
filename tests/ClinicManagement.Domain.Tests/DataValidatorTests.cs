using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Tests;

public class DataValidatorTests
{
    [Theory]
    [InlineData("patient@example.com")]
    [InlineData(" patient@example.com ")]
    public void IsValidEmail_ReturnsTrueForValidEmail(string email)
    {
        Assert.True(DataValidator.IsValidEmail(email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("patient@")]
    public void IsValidEmail_ReturnsFalseForInvalidEmail(string? email)
    {
        Assert.False(DataValidator.IsValidEmail(email));
    }

    [Theory]
    [InlineData("d2719f3a-3bd3-4f4a-a2c2-9e4b7f59c660")]
    [InlineData("{d2719f3a-3bd3-4f4a-a2c2-9e4b7f59c660}")]
    public void IsValidGuid_ReturnsTrueForValidGuid(string value)
    {
        Assert.True(DataValidator.IsValidGuid(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("not-a-guid")]
    public void IsValidGuid_ReturnsFalseForInvalidGuid(string? value)
    {
        Assert.False(DataValidator.IsValidGuid(value));
    }

    [Fact]
    public void IsValidGuid_ReturnsFalseForEmptyGuid()
    {
        Assert.False(DataValidator.IsValidGuid(Guid.Empty));
    }

    [Fact]
    public void PatientCreation_RejectsEmptyGuid()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Patient.Create(
                Guid.Empty,
                "Test",
                "Patient",
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
                "test@example.com",
                "555-0100"));

        Assert.Equal("id", exception.PropertyName);
    }

    [Theory]
    [InlineData(AllergySeverity.MILD)]
    [InlineData(AllergySeverity.MODERATE)]
    [InlineData(AllergySeverity.SEVERE)]
    public void IsValidSeverity_ReturnsTrueForDefinedValues(AllergySeverity severity)
    {
        Assert.True(DataValidator.IsValidSeverity(severity));
    }

    [Fact]
    public void IsValidSeverity_ReturnsFalseForUndefinedValue()
    {
        Assert.False(DataValidator.IsValidSeverity((AllergySeverity)999));
    }

    [Fact]
    public void IsValidDateOfBirth_ReturnsFalseForTodayOrLater()
    {
        var today = new DateOnly(2030, 1, 1);

        Assert.False(DataValidator.IsValidDateOfBirth(today, today));
        Assert.False(DataValidator.IsValidDateOfBirth(today.AddDays(1), today));
    }

    [Fact]
    public void IsFutureDateTime_RequiresValueAfterCurrentTime()
    {
        var currentTime = new DateTimeOffset(2030, 1, 1, 9, 0, 0, TimeSpan.Zero);

        Assert.True(DataValidator.IsFutureDateTime(currentTime.AddTicks(1), currentTime));
        Assert.False(DataValidator.IsFutureDateTime(currentTime, currentTime));
        Assert.False(DataValidator.IsFutureDateTime(currentTime.AddTicks(-1), currentTime));
    }

    [Fact]
    public void PatientAllergyCreation_RejectsUndefinedSeverity()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            PatientAllergy.Create("Peanuts", (AllergySeverity)999));

        Assert.Equal("severity", exception.PropertyName);
    }

    [Fact]
    public void PatientCreation_RejectsInvalidEmail()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Patient.Create(
                Guid.NewGuid(),
                "Test",
                "Patient",
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
                "not-an-email",
                "555-0100"));

        Assert.Equal("email", exception.PropertyName);
    }
}
