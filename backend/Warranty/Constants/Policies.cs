namespace Warranty.Constants;

/// <summary>
/// Authorization policy names registered in Program.cs (Buổi 9).
/// Prefer [Authorize(Policy = Policies.X)] over raw role strings.
/// </summary>
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string AdminOrManager = "AdminOrManager";
    public const string Staff = "Staff";
    public const string InternalStaff = "InternalStaff";
    public const string CanManageUsers = "CanManageUsers";
    public const string CanManageCatalog = "CanManageCatalog";
    public const string CanManageServiceCenters = "CanManageServiceCenters";
    public const string CanAssignTechnician = "CanAssignTechnician";
}
