using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EventForge.Contract.Entities
{
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

}
