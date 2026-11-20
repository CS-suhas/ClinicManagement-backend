using Dapper;

namespace Infrastructure.Data;

public static class OracleDapperTypeHandlers
{
    public static void Register()
    {
        SqlMapper.RemoveTypeMap(typeof(DateOnly));
        SqlMapper.AddTypeHandler<Guid>(new OracleGuidTypeHandler());
        SqlMapper.AddTypeHandler<DateOnly>(new OracleDateOnlyTypeHandler());
        SqlMapper.AddTypeHandler<DateTimeOffset>(new OracleDateTimeOffsetTypeHandler());
        SqlMapper.PurgeQueryCache();
    }
}
