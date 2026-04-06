using System;
using System.ComponentModel.DataAnnotations;


namespace Example.OPUS.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class StrictEmailAttribute : ValidationAttribute
    {
        public StrictEmailAttribute()
        {
            ErrorMessage = "The {0} must be a valid email address.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return null;

            if (!(value is string email) || string.IsNullOrWhiteSpace(email))
                return new ValidationResult("Email must be a non-empty string.");

            // Very strict practical regex
            const string pattern =
                @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";

            if (!System.Text.RegularExpressions.Regex.IsMatch(email, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                string fieldName = validationContext?.DisplayName ?? validationContext?.MemberName ?? "email";
                return new ValidationResult(FormatErrorMessage(fieldName));
            }

            return null;
        }
    }
}