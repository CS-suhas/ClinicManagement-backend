namespace ClinicManagement.Api.Security;

public static class ClinicClaimTypes
{
    /// <summary>The signed-in user's <c>Users</c> row id, added by <see cref="RoleClaimsTransformation"/>.</summary>
    public const string UserId = "clinic_user_id";
}
