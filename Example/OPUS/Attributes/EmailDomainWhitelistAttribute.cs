using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;


namespace Example.OPUS.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class EmailDomainWhitelistAttribute : ValidationAttribute
    {
        private readonly string[] _allowedDomains;

        public EmailDomainWhitelistAttribute(params string[] allowedDomains)
        {
            _allowedDomains = allowedDomains ?? throw new ArgumentNullException(nameof(allowedDomains));
            ErrorMessage = "The {0} must use an allowed domain: " + string.Join(", ", allowedDomains) + ".";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return null;

            if (!(value is string email) || string.IsNullOrWhiteSpace(email))
                return new ValidationResult("Email must be a non-empty string.");

            try
            {
                string domain = email.Split('@')[1].ToLowerInvariant();

                if (!_allowedDomains.Any(d => domain.EndsWith(d.ToLowerInvariant())))
                {
                    string fieldName = validationContext?.DisplayName ?? validationContext?.MemberName ?? "email";
                    return new ValidationResult(FormatErrorMessage(fieldName));
                }

                return null;
            }
            catch
            {
                return new ValidationResult("Invalid email format.");
            }
        }
    }
}