using System;
using System.ComponentModel.DataAnnotations;


namespace Example.OPUS.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ValidUUIDAttribute : ValidationAttribute
    {
        public ValidUUIDAttribute()
        {
            ErrorMessage = "The {0} must be a valid UUID/GUID.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return null;

            if (!(value is string strValue) || string.IsNullOrWhiteSpace(strValue))
                return new ValidationResult("The value must be a non-empty string.");

            if (!Guid.TryParse(strValue, out _))
            {
                string fieldName = validationContext?.DisplayName ?? validationContext?.MemberName ?? "UUID";
                return new ValidationResult(FormatErrorMessage(fieldName));
            }

            return null;
        }
    }
}
