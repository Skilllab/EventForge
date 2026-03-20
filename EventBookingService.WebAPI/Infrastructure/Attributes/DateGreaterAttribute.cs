using System.ComponentModel.DataAnnotations;

namespace EventBookingService.WebAPI.Infrastructure.Attributes
{
    /// <summary>
    /// Атрибут проверки дат
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class DateGreaterAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateGreaterAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var curDate = (DateTime?)value;
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (property == null)
                throw new ArgumentException("Свойство не найдено");

            var comparisonDate = (DateTime?)property.GetValue(validationContext.ObjectInstance);

            if (curDate.HasValue && comparisonDate.HasValue && curDate < comparisonDate)
            {
                return new ValidationResult(ErrorMessage ?? "Дата окончания события не может быть раньше даты начала",
                    new[] { validationContext.MemberName! }
                );
            }

            return ValidationResult.Success;
        }

        public override bool IsValid(object value)
        {
            throw new NotImplementedException();
        }
    }
}
