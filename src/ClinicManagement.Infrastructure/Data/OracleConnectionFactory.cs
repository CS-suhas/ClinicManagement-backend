using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace ClinicManagement.Infrastructure.Data;

public class OracleConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        return new OracleConnection(connectionString) { BindByName = true };
    }
}
