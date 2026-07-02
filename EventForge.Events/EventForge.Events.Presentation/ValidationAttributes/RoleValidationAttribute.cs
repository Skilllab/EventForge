using System.ComponentModel.DataAnnotations;

using EventForge.Users.Domain.Entities;

namespace EventForge.Events.Presentation.ValidationAttributes
{
    public class RoleValidationAttribute : ValidationAttribute
    {
        private static readonly HashSet<string> _validRoles =
            new HashSet<string>(Enum.GetNames(typeof(RoleType)), StringComparer.OrdinalIgnoreCase);

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || !_validRoles.Contains(value.ToString()))
            {
                return new ValidationResult($"Недопустимая роль. Допустимые значения: {string.Join(", ", _validRoles)}");
            }
            return ValidationResult.Success;
        }
    }
}
