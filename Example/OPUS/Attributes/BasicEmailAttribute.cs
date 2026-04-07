using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;


namespace Puma.MDE.OPUS.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class BasicEmailAttribute : ValidationAttribute
    {
        public BasicEmailAttribute()
        {
            ErrorMessage = "The {0} must be a valid email address.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return null; // success (combine with [Required] if needed)

            if (!(value is string email))
                return new ValidationResult("The value must be a string.");

            if (string.IsNullOrWhiteSpace(email))
                return new ValidationResult("Email cannot be empty.");

            // Standard email regex (RFC 5322-ish, practical version)
            const string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            if (!Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase))
            {
                string fieldName = validationContext?.DisplayName ?? validationContext?.MemberName ?? "email";
                return new ValidationResult(FormatErrorMessage(fieldName));
            }

            return null;
        }
    }
}