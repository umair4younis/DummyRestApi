
namespace Puma.MDE.OPUS.Models
{
    public class ValidationResultOpus
    {
        public bool IsValid { get; set; }

        public string AssetName { get; set; }

        public string AssetUuid { get; set; }

        public string ErrorMessage { get; set; }
    }
}
