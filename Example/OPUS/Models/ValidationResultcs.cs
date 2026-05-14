
namespace Puma.MDE.OPUS.Models
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }

        public string AssetName { get; set; }

        public string AssetUuid { get; set; }

        public string ErrorMessage { get; set; }
    }
}
