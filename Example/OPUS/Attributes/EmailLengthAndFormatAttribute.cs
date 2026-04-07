using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;


namespace Puma.MDE.OPUS.Attributes
{
    /// <summary>
    /// Validates that the value is a valid email address and its length is within the specified range.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class EmailLengthAndFormatAttribute : ValidationAttribute
    {
        private readonly int _minLength;
        private readonly int _maxLength;

        public EmailLengthAndFormatAttribute(int minLength = 5, int maxLength = 254)
        {
            _minLength = minLength;
            _maxLength = maxLength;

            ErrorMessage = $"The {{0}} must be a valid email address with length between {minLength} and {maxLength} characters.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // Allow null (combine with [Required] if mandatory)
            if (value == null)
            {
                return null;
            }

            if (!(value is string email) || string.IsNullOrWhiteSpace(email))
            {
                return new ValidationResult("The value must be a non-empty string.");
            }

            string trimmedEmail = email.Trim();

            // Length check
            if (trimmedEmail.Length < _minLength || trimmedEmail.Length > _maxLength)
            {
                string fieldName = validationContext?.DisplayName ?? validationContext?.MemberName ?? "email";
                return new ValidationResult(FormatErrorMessage(fieldName));
            }

            // Format check - practical regex for most real-world emails
            const string emailPattern =
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$";  // basic: local@domain.tld

            // More strict version (optional - uncomment if needed)
            // const string emailPattern = 
            //     @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
            //     @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";

            if (!Regex.IsMatch(trimmedEmail, emailPattern, RegexOptions.IgnoreCase))
            {
                string fieldName = validationContext?.DisplayName ?? validationContext?.MemberName ?? "email";
                return new ValidationResult(FormatErrorMessage(fieldName));
            }

            return null; // success
        }
    }
}