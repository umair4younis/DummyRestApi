using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Example.OPUS.Models
{
    /// <summary>
    /// Represents the asset tree types array wrapper.
    /// </summary>
    public class AssetTreeTypes
    {
        [JsonProperty("value")]
        [Required(ErrorMessage = "Asset tree types value is required")]
        [MinLength(1, ErrorMessage = "At least one asset tree type must be specified")]
        public List<string> Value { get; set; }
    }
}
