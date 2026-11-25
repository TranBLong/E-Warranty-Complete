namespace Warranty.Constants;

/// <summary>
/// Centralized role name constants for [Authorize(Roles = ...)] and policy registration.
/// Keep in sync with <see cref="Enums.UserRole"/>.
/// </summary>
public static class Roles
{
    public const string Admin = nameof(Enums.UserRole.Admin);
    public const string Manager = nameof(Enums.UserRole.Manager);
    public const string Receptionist = nameof(Enums.UserRole.Receptionist);
    public const string Technician = nameof(Enums.UserRole.Technician);
    public const string Customer = nameof(Enums.UserRole.Customer);

    /// <summary>Admin + Manager</summary>
    public const string AdminOrManager = $"{Admin},{Manager}";

    /// <summary>Admin + Manager + Receptionist</summary>
    public const string Staff = $"{Admin},{Manager},{Receptionist}";

    /// <summary>Admin + Manager + Receptionist + Technician</summary>
    public const string InternalStaff = $"{Admin},{Manager},{Receptionist},{Technician}";
}
