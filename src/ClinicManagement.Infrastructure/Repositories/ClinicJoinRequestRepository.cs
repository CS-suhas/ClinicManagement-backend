using ClinicManagement.Application.ClinicJoinRequests.Interfaces;
using ClinicManagement.Application.ClinicJoinRequests.Models;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;
using Dapper;
using Infrastructure.Data;
using Oracle.ManagedDataAccess.Client;

namespace ClinicManagement.Infrastructure.Repositories;

public class ClinicJoinRequestRepository(IDbConnectionFactory connectionFactory) : IClinicJoinRequestRepository
{
    private const int UniqueConstraintViolation = 1; // ORA-00001

    private const string SelectResponse = """
        SELECT r.Id,
               r.ProviderId,
               pr.FirstName || ' ' || pr.LastName AS ProviderName,
               pr.Specialty AS ProviderSpecialty,
               r.ClinicId,
               c.Name AS ClinicName,
               r.Status,
               r.CreatedAtUtc,
               r.DecidedAtUtc
        FROM ClinicJoinRequests r
        INNER JOIN Providers pr ON pr.Id = r.ProviderId
        INNER JOIN Clinics c ON c.Id = r.ClinicId
        """;

    public async Task<ClinicJoinRequest?> GetByIdAsync(Guid requestId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT Id, ProviderId, ClinicId, Status, DecidedAtUtc, CreatedAtUtc, UpdatedAtUtc
            FROM ClinicJoinRequests
            WHERE Id = :Id
            """;
        return await connection.QuerySingleOrDefaultAsync<ClinicJoinRequest>(
            sql,
            new { Id = requestId.ToString() });
    }

    public async Task<IReadOnlyList<ClinicJoinRequestResponse>> GetByProviderIdAsync(Guid providerId)
    {
        using var connection = connectionFactory.CreateConnection();
        var requests = await connection.QueryAsync<ClinicJoinRequestResponse>(
            $"""
            {SelectResponse}
            WHERE r.ProviderId = :ProviderId
            ORDER BY r.CreatedAtUtc DESC, r.Id
            """,
            new { ProviderId = providerId.ToString() });
        return requests.AsList();
    }

    public async Task<IReadOnlyList<ClinicJoinRequestResponse>> GetByClinicIdAsync(
        Guid clinicId,
        ClinicJoinRequestStatus? status)
    {
        using var connection = connectionFactory.CreateConnection();
        var requests = await connection.QueryAsync<ClinicJoinRequestResponse>(
            $"""
            {SelectResponse}
            WHERE r.ClinicId = :ClinicId
              AND (:Status IS NULL OR r.Status = :Status)
            ORDER BY r.CreatedAtUtc DESC, r.Id
            """,
            new { ClinicId = clinicId.ToString(), Status = status?.ToString() });
        return requests.AsList();
    }

    public async Task<bool> HasPendingAsync(Guid providerId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT COUNT(1) FROM ClinicJoinRequests
            WHERE ProviderId = :ProviderId AND Status = 'PENDING'
            """;
        return await connection.ExecuteScalarAsync<int>(sql, new { ProviderId = providerId.ToString() }) > 0;
    }

    public async Task<Guid> AddAsync(ClinicJoinRequest request)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO ClinicJoinRequests (Id, ProviderId, ClinicId, Status)
            VALUES (:Id, :ProviderId, :ClinicId, :Status)
            """;
        try
        {
            await connection.ExecuteAsync(sql, new
            {
                Id = request.Id.ToString(),
                ProviderId = request.ProviderId.ToString(),
                ClinicId = request.ClinicId.ToString(),
                Status = request.Status.ToString()
            });
        }
        catch (OracleException exception) when (exception.Number == UniqueConstraintViolation)
        {
            // Lost a race with a concurrent request: the unique "one pending per provider" index fired.
            throw new ClinicJoinRequestAlreadyPendingException(request.ProviderId);
        }
        return request.Id;
    }

    public async Task<bool> TryApproveAsync(ClinicJoinRequest approvedRequest)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var requestRows = await connection.ExecuteAsync(
            DecideSql,
            DecideParameters(approvedRequest),
            transaction);
        if (requestRows != 1)
        {
            transaction.Rollback();
            return false;
        }

        // The provider must still be verified and not already at the target clinic.
        const string assignSql = """
            UPDATE Providers
            SET ClinicId = :ClinicId, UpdatedAtUtc = SYSTIMESTAMP
            WHERE Id = :ProviderId
              AND ClinicId <> :ClinicId
              AND VerificationStatus = 'VERIFIED'
            """;
        var providerRows = await connection.ExecuteAsync(
            assignSql,
            new
            {
                ClinicId = approvedRequest.ClinicId.ToString(),
                ProviderId = approvedRequest.ProviderId.ToString()
            },
            transaction);
        if (providerRows != 1)
        {
            transaction.Rollback();
            return false;
        }

        transaction.Commit();
        return true;
    }

    public async Task<bool> TryDecideAsync(ClinicJoinRequest decidedRequest)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(DecideSql, DecideParameters(decidedRequest));
        return rows == 1;
    }

    private const string DecideSql = """
        UPDATE ClinicJoinRequests
        SET Status = :Status, DecidedAtUtc = :DecidedAtUtc, UpdatedAtUtc = SYSTIMESTAMP
        WHERE Id = :Id AND Status = 'PENDING'
        """;

    private static object DecideParameters(ClinicJoinRequest request) => new
    {
        Id = request.Id.ToString(),
        Status = request.Status.ToString(),
        request.DecidedAtUtc
    };
}
