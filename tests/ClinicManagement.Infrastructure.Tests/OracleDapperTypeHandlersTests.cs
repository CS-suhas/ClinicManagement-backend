using System.Data;
using Dapper;
using ClinicManagement.Infrastructure.Data;
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

    [Fact]
    public void DateTimeOffsetHandler_BindsUtcTimestampWithFullDateTimePrecision()
    {
        var handler = new OracleDateTimeOffsetTypeHandler();
        var parameter = new OracleParameter();
        var value = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero).AddTicks(1234567);

        handler.SetValue(parameter, value);

        Assert.Equal(OracleDbType.TimeStamp, parameter.OracleDbType);
        Assert.Equal(value.UtcDateTime, parameter.Value);
    }
}
