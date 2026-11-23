namespace ClinicManagement.Infrastructure.Repositories;

/// <summary>
/// Column list and joins shared by every query that returns <c>AppointmentListResponse</c>, so the allergy summary
/// and status timestamps are computed the same way everywhere. The allergy summary is one grouped derived table
/// joined on patient, so no per-row lookups are made.
/// </summary>
internal static class AppointmentListSql
{
    public const string Columns = """
        SELECT a.Id,
               a.PatientId,
               p.FirstName || ' ' || p.LastName AS PatientName,
               a.ProviderId,
               pr.FirstName || ' ' || pr.LastName AS ProviderName,
               pr.Specialty AS ProviderSpecialty,
               pr.ClinicId,
               c.Name AS ClinicName,
               a.AppointmentDateTime,
               a.ReasonForVisit,
               a.Status,
               NVL(allergySummary.AllergyCount, 0) AS PatientAllergyCount,
               CASE allergySummary.SeverityRank
                   WHEN 3 THEN 'SEVERE'
                   WHEN 2 THEN 'MODERATE'
                   WHEN 1 THEN 'MILD'
               END AS PatientHighestAllergySeverity,
               a.CreatedAtUtc,
               COALESCE(a.StatusChangedAtUtc, a.CreatedAtUtc) AS StatusChangedAtUtc
        """;

    public const string Joins = """
        FROM Appointments a
        INNER JOIN Patients p ON p.Id = a.PatientId
        INNER JOIN Providers pr ON pr.Id = a.ProviderId
        INNER JOIN Clinics c ON c.Id = pr.ClinicId
        LEFT JOIN (
            SELECT PatientId,
                   COUNT(1) AS AllergyCount,
                   MAX(CASE Severity WHEN 'SEVERE' THEN 3 WHEN 'MODERATE' THEN 2 WHEN 'MILD' THEN 1 ELSE 0 END) AS SeverityRank
            FROM PatientAllergies
            GROUP BY PatientId
        ) allergySummary ON allergySummary.PatientId = a.PatientId
        """;
}
