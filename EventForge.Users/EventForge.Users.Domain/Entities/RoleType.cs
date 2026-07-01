using System.ComponentModel;

namespace EventForge.Users.Domain.Entities;

/// <summary>
/// Роли пользователя в системе.
/// </summary>
public enum RoleType
{
    /// <summary>
    /// Роль обычного пользователя.
    /// </summary>
    [Description("Роль обычного пользователя")]
    User,

    /// <summary>
    /// Роль администратора.
    /// </summary>
    [Description("Роль администратора")]
    Admin,
}
