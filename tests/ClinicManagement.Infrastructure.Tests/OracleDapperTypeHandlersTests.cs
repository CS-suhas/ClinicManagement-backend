using System.Data;
using Dapper;
using Infrastructure.Data;
using Oracle.ManagedDataAccess.Client;

namespace ClinicManagement.Infrastructure.Tests;

public class OracleDapperTypeHandlersTests
{
    [Fact]
    public void Register_UsesOracleDateOnlyHandlerForDateOnlyParameters()
    {
        OracleDapperTypeHandlers.Register();

#pragma warning disable CS0618
        var dbType = SqlMapper.LookupDbType(
            typeof(DateOnly),
            "DateOfBirth",
            demand: false,
            out var handler);
#pragma warning restore CS0618

        Assert.Equal(DbType.Object, dbType);
        Assert.IsType<OracleDateOnlyTypeHandler>(handler);

        var parameter = new OracleParameter();
        handler!.SetValue(parameter, new DateOnly(1985, 12, 10));

        Assert.Equal(OracleDbType.Date, parameter.OracleDbType);
        Assert.Equal(new DateTime(1985, 12, 10), parameter.Value);
    }
}
