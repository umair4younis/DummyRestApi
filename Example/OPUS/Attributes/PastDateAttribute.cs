using System;
using System.ComponentModel.DataAnnotations;


namespace Puma.MDE.OPUS.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PastDateAttribute : ValidationAttribute
    {
        public PastDateAttribute()
        {
            ErrorMessage = "The {0} must be a date in the past.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return null;

            if (!(value is DateTime dateValue))
                return new ValidationResult("The value must be a valid DateTime.");

            if (dateValue.Date >= DateTime.Today)
            {
                string fieldName = validationContext?.DisplayName ?? validationContext?.MemberName ?? "date";
                return new ValidationResult(FormatErrorMessage(fieldName));
            }

            return null;
        }
    }
}
