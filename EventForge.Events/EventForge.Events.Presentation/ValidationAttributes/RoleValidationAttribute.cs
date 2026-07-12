using System.ComponentModel.DataAnnotations;

using EventForge.Shared.Enums;

namespace EventForge.Events.Presentation.ValidationAttributes;

/// <summary>
/// Атрибут валидации, который проверяет, что роль пользователя соответствует указанной роли
/// </summary>
public class RoleValidationAttribute : ValidationAttribute
{
    private static readonly HashSet<string> _validRoles =
        new HashSet<string>(Enum.GetNames(typeof(RoleType)), StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null || !_validRoles.Contains(value.ToString()))
        {
            return new ValidationResult($"Недопустимая роль. Допустимые значения: {string.Join(", ", _validRoles)}");
        }
        return ValidationResult.Success;
    }
}
