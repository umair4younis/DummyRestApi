using System;
using System.ComponentModel.DataAnnotations;


namespace Example.OPUS.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FutureDateAttribute : ValidationAttribute
    {
        public FutureDateAttribute()
        {
            ErrorMessage = "The {0} must be a date in the future.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return null; // success (combine with [Required] if needed)
            }

            if (!(value is DateTime dateValue))
            {
                return new ValidationResult("The value must be a valid DateTime.");
            }

            if (dateValue.Date <= DateTime.Today)
            {
                string fieldName = validationContext?.DisplayName ?? validationContext?.MemberName ?? "date";
                string error = FormatErrorMessage(fieldName);
                return new ValidationResult(error);
            }

            return null;
        }
    }
}