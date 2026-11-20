using System.Data;
using Dapper;

namespace Infrastructure.Data;

public sealed class OracleDateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override DateTimeOffset Parse(object value)
    {
        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => dateTime,
            string text => DateTimeOffset.Parse(text),
            _ => throw new DataException($"Cannot convert Oracle value of type '{value.GetType().FullName}' to DateTimeOffset.")
        };
    }

    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.DbType = DbType.DateTime;
        parameter.Value = value.UtcDateTime;
    }
}
