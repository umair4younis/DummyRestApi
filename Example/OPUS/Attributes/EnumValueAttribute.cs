using System;
using System.ComponentModel.DataAnnotations;


namespace Example.OPUS.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class EnumValueAttribute : ValidationAttribute
    {
        private readonly Type _enumType;

        public EnumValueAttribute(Type enumType)
        {
            _enumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
            ErrorMessage = "The {0} must be a valid value of " + enumType.Name + ".";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return null;

            if (!Enum.IsDefined(_enumType, value))
            {
                string fieldName = validationContext?.DisplayName ?? validationContext?.MemberName ?? "value";
                return new ValidationResult(FormatErrorMessage(fieldName));
            }

            return null;
        }
    }
}
