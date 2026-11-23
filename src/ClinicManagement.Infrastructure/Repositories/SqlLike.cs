namespace ClinicManagement.Infrastructure.Repositories;

/// <summary>
/// Makes user text safe to use inside a LIKE pattern. Every LIKE that takes user text must be written as
/// <c>LIKE '%' || UPPER(:Param) || '%' ESCAPE '\'</c> with the parameter passed through <see cref="Escape"/>,
/// so that <c>%</c>, <c>_</c> and <c>\</c> in the text match themselves rather than acting as wildcards.
/// </summary>
internal static class SqlLike
{
    public static string? Escape(string? text) =>
        text?.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
