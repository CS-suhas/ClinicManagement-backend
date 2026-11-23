using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Repositories;

public class SearchRepository(IDbConnectionFactory connectionFactory) : ISearchRepository
{
    // Case-insensitive contains-match on the name parts and the email, as the other search queries do.
    private const string PatientMatch = """
        (UPPER(p.FirstName) LIKE '%' || UPPER(:Term) || '%' ESCAPE '\'
          OR UPPER(p.LastName) LIKE '%' || UPPER(:Term) || '%' ESCAPE '\'
          OR UPPER(p.FirstName || ' ' || p.LastName) LIKE '%' || UPPER(:Term) || '%' ESCAPE '\'
          OR UPPER(p.Email) LIKE '%' || UPPER(:Term) || '%' ESCAPE '\')
        """;

    private const string ProviderMatch = """
        (UPPER(pr.FirstName) LIKE '%' || UPPER(:Term) || '%' ESCAPE '\'
          OR UPPER(pr.LastName) LIKE '%' || UPPER(:Term) || '%' ESCAPE '\'
          OR UPPER(pr.FirstName || ' ' || pr.LastName) LIKE '%' || UPPER(:Term) || '%' ESCAPE '\'
          OR UPPER(pr.Specialty) LIKE '%' || UPPER(:Term) || '%' ESCAPE '\')
        """;

    private const string ClinicMatch = """
        (UPPER(c.Name) LIKE '%' || UPPER(:Term) || '%' ESCAPE '\'
          OR UPPER(c.Address) LIKE '%' || UPPER(:Term) || '%' ESCAPE '\')
        """;

    public async Task<IReadOnlyList<SearchPatientResult>> SearchPatientsAsync(
        string term,
        int limit,
        PatientSearchScope scope)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Term", SqlLike.Escape(term));
        parameters.Add("Limit", limit);

        var visibility = string.Empty;
        if (!scope.AllPatients)
        {
            var conditions = new List<string>();
            if (scope.ProviderId is { } providerId)
            {
                conditions.Add("pr.Id = :ProviderId");
                parameters.Add("ProviderId", providerId.ToString());
            }

            if (scope.ClinicIds.Count > 0)
            {
                conditions.Add("pr.ClinicId IN :ClinicIds");
                parameters.Add("ClinicIds", scope.ClinicIds.Select(id => id.ToString()).ToArray());
            }

            if (conditions.Count == 0)
                return [];

            visibility = $"""
                AND EXISTS (
                    SELECT 1
                    FROM Appointments a
                    INNER JOIN Providers pr ON pr.Id = a.ProviderId
                    WHERE a.PatientId = p.Id AND ({string.Join(" OR ", conditions)})
                )
                """;
        }

        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<SearchPatientResult>(
            $"""
            SELECT p.Id, p.FirstName || ' ' || p.LastName AS Name, p.Email AS Subtitle
            FROM Patients p
            WHERE {PatientMatch}
            {visibility}
            ORDER BY UPPER(p.FirstName || ' ' || p.LastName), p.Id
            OFFSET 0 ROWS FETCH NEXT :Limit ROWS ONLY
            """,
            parameters);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<SearchProviderResult>> SearchProvidersAsync(
        string term,
        int limit,
        bool verifiedOnly)
    {
        var verifiedFilter = verifiedOnly ? "AND pr.VerificationStatus = 'VERIFIED'" : string.Empty;

        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<SearchProviderResult>(
            $"""
            SELECT pr.Id, pr.FirstName || ' ' || pr.LastName AS Name, pr.Specialty, c.Name AS ClinicName
            FROM Providers pr
            LEFT JOIN Clinics c ON c.Id = pr.ClinicId
            WHERE {ProviderMatch}
            {verifiedFilter}
            ORDER BY UPPER(pr.FirstName || ' ' || pr.LastName), pr.Id
            OFFSET 0 ROWS FETCH NEXT :Limit ROWS ONLY
            """,
            new { Term = SqlLike.Escape(term), Limit = limit });
        return rows.AsList();
    }

    public async Task<IReadOnlyList<SearchClinicResult>> SearchClinicsAsync(string term, int limit)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<SearchClinicResult>(
            $"""
            SELECT c.Id, c.Name, c.Address
            FROM Clinics c
            WHERE {ClinicMatch}
            ORDER BY UPPER(c.Name), c.Id
            OFFSET 0 ROWS FETCH NEXT :Limit ROWS ONLY
            """,
            new { Term = SqlLike.Escape(term), Limit = limit });
        return rows.AsList();
    }
}
