using System.Data;

namespace ClinicManagement.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}