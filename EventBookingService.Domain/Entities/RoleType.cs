using System.ComponentModel;

namespace EventBookingService.Domain.Entities;

public enum RoleType
{
    /// <summary>
    /// Роль обычного пользователя
    /// </summary>
    [Description("Роль обычного пользователя")]
    User,
    /// <summary>
    /// Роль администратора
    /// </summary>
    [Description("Роль администратора")]
    Admin
}