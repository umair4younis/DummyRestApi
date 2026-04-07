using System;
using System.ComponentModel.DataAnnotations;
using System.Net;


namespace Puma.MDE.OPUS.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class EmailDomainResolvableAttribute : ValidationAttribute
    {
        public EmailDomainResolvableAttribute()
        {
            ErrorMessage = "The {0} domain does not resolve (no valid DNS records found).";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return null;

            if (!(value is string email) || string.IsNullOrWhiteSpace(email))
                return new ValidationResult("Email must be a non-empty string.");

            try
            {
                // Extract domain part
                string domain = email.Split('@')[1].Trim();

                // Try to resolve any host entry (A/AAAA)
                var hostEntry = Dns.GetHostEntry(domain);

                // If we reach here → domain has DNS records
                if (hostEntry.AddressList.Length == 0)
                {
                    throw new Exception("No IP addresses resolved.");
                }

                return null; // success
            }
            catch (Exception ex)
            {
                string fieldName = validationContext?.DisplayName ?? validationContext?.MemberName ?? "email";
                string error = FormatErrorMessage(fieldName) + " (" + ex.Message + ")";
                return new ValidationResult(error);
            }
        }
    }
}