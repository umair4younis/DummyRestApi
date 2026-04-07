using System;
using System.ComponentModel.DataAnnotations;


namespace Puma.MDE.OPUS.Models
{
    /// <summary>
    /// Ensures the date is not in the past (useful for quote date/time).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NotInPastAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return null;

            if (!(value is DateTime date))
                return new ValidationResult("Value must be a valid DateTime.");

            if (date.Date < DateTime.Today)
            {
                string name = validationContext?.DisplayName ?? validationContext?.MemberName ?? "date";
                return new ValidationResult($"The {name} cannot be in the past.");
            }

            return null;
        }
    }
}