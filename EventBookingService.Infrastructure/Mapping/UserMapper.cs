using EventBookingService.Domain.Entities;
using EventBookingService.Infrastructure.Entities;

namespace EventBookingService.Infrastructure.Mapping
{
    public static class UserMapper
    {
        /// <summary>
        /// Из сущности БД в доменную модель
        /// </summary>
        public static User ToDomain(this UserEntity entity)
        {
            var role = Enum.Parse<RoleType>(entity.Role);
            var domain = User.Create(entity.Login, entity.PasswordHash, role);

            //Восстанавливаем состояние, которое закрыто для изменений извне
            var type = typeof(User);
            type.GetProperty(nameof(User.Id))?.SetValue(domain, entity.Id);

            return domain;
        }

        /// <summary>
        /// Из домена в сущность БД
        /// </summary>
        public static UserEntity ToEntity(this User domain) =>
            new()
            {
                Id = domain.Id,
                Login = domain.Login,
                PasswordHash = domain.PasswordHash,
                Role = domain.Role.ToString(),
            };

    }
}
