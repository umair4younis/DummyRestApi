using System;
using System.ComponentModel.DataAnnotations;


namespace Puma.MDE.OPUS.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PositiveNumberAttribute : ValidationAttribute
    {
        public PositiveNumberAttribute()
        {
            ErrorMessage = "The {0} must be a positive number.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return null;

            decimal? num = null;

            if (value is decimal d) num = d;
            else if (value is double dbl) num = (decimal)dbl;
            else if (value is int i) num = i;
            else if (value is long l) num = l;

            if (!num.HasValue)
                return new ValidationResult("The value must be a numeric type.");

            if (num.Value <= 0)
            {
                string fieldName = validationContext?.DisplayName ?? validationContext?.MemberName ?? "value";
                return new ValidationResult(FormatErrorMessage(fieldName));
            }

            return null;
        }
    }
}
