using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Example.OPUS.Models
{
    /// <summary>
    /// Shared value object for monetary, percentage, price, or quantity amounts.
    /// Used for: dividendAmount, nominal, initialPrice, spread, volume.
    /// </summary>
    public class AmountValue
    {
        [JsonProperty("quantity")]
        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be positive")]
        public decimal Quantity { get; set; }

        [JsonProperty("unit")]
        [Required(ErrorMessage = "Unit is required")]
        [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
        public string Unit { get; set; }

        [JsonProperty("type")]
        [Required(ErrorMessage = "Type is required")]
        [RegularExpression("^(PERCENT|MONEY|PRICE_PER_PIECE|PIECE)$", ErrorMessage = "Invalid amount type")]
        public string Type { get; set; }

        // Optional: parameterless constructor for deserialization
        public AmountValue() { }

        // Convenience constructor
        public AmountValue(decimal quantity, string unit, string type)
        {
            Quantity = quantity;
            Unit = unit;
            Type = type;
        }

        // Optional helpers for common patterns
        public static AmountValue FromPercent(decimal percent)
            => new AmountValue(percent, "%", "PERCENT");

        public static AmountValue FromMoney(decimal amount, string currency)
            => new AmountValue(amount, currency, "MONEY");

        public static AmountValue FromPricePerPiece(decimal price, string currency = "USD")
            => new AmountValue(price, $"{currency}/Pieces", "PRICE_PER_PIECE");

        public static AmountValue FromPieces(decimal count)
            => new AmountValue(count, "Pieces", "PIECE");
    }
}
