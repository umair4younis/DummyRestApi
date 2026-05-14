using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    public class PercentAmountValue
    {
        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        public PercentAmountValue() { }

        public PercentAmountValue(decimal quantity, string unit = "%")
        {
            Quantity = quantity;
            Unit = unit;
        }

        public static PercentAmountValue FromPercent(decimal value)
            => new PercentAmountValue(value, "%");

        public override string ToString()
            => string.Format("{0:G} {1}", Quantity, Unit ?? "%");
    }
}