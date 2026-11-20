using System.Data;
using Dapper;

namespace Infrastructure.Data;

public sealed class OracleGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value)
    {
        return value switch
        {
            Guid guid => guid,
            string text => Guid.Parse(text),
            _ => throw new DataException($"Cannot convert Oracle value of type '{value.GetType().FullName}' to Guid.")
        };
    }

    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString("D");
    }
}
