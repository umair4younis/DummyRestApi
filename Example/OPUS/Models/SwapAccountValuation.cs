using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Puma.MDE.OPUS.Attributes;


namespace Puma.MDE.OPUS.Models
{
    /// <summary>
    /// Full model for creating or replacing a Total Return Swap (TRS).
    /// </summary>
    public class SwapAccountValuation
    {
        [JsonProperty("accountSegment")]
        [Required(ErrorMessage = "Account segment is required")]
        [StringLength(36, MinimumLength = 36, ErrorMessage = "Account segment must be a valid UUID (36 characters)")]
        public string AccountSegment { get; set; }

        [JsonProperty("asset")]
        [Required(ErrorMessage = "Asset identifier is required")]
        [StringLength(36, MinimumLength = 36, ErrorMessage = "Asset must be a valid UUID (36 characters)")]
        public string Asset { get; set; }

        [JsonProperty("assetClassLiteral")]
        [Required(ErrorMessage = "Asset class literal is required")]
        [StringLength(50, ErrorMessage = "Asset class literal cannot exceed 50 characters")]
        public string AssetClassLiteral { get; set; }

        [JsonProperty("assetTreeTypes")]
        [Required(ErrorMessage = "Asset tree types are required")]
        public AssetTreeTypes AssetTreeTypes { get; set; }

        [JsonProperty("businessHolidayFixingLeg1")]
        [StringLength(50, ErrorMessage = "Business holiday fixing rule cannot exceed 50 characters")]
        public string BusinessHolidayFixingLeg1 { get; set; }

        [JsonProperty("businessHolidayFixingLeg2")]
        [StringLength(50, ErrorMessage = "Business holiday fixing rule cannot exceed 50 characters")]
        public string BusinessHolidayFixingLeg2 { get; set; }

        [JsonProperty("counterpart")]
        [StringLength(50, ErrorMessage = "Counterpart identifier cannot exceed 50 characters")]
        public string Counterpart { get; set; }

        [JsonProperty("currency")]
        [Required(ErrorMessage = "Currency is required")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter ISO code")]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency must be a valid 3-letter uppercase ISO code")]
        public string Currency { get; set; }

        [JsonProperty("dayCountConvention")]
        [StringLength(20, ErrorMessage = "Day count convention cannot exceed 20 characters")]
        public string DayCountConvention { get; set; }

        [JsonProperty("designatedMaturity")]
        [StringLength(20, ErrorMessage = "Designated maturity cannot exceed 20 characters")]
        [RegularExpression(@"^P[0-9]+[DWMY]$", ErrorMessage = "Designated maturity must be in ISO 8601 duration format (e.g., P1Y, P6M)")]
        public string DesignatedMaturity { get; set; }

        [JsonProperty("directionLeg1")]
        [Required(ErrorMessage = "Direction Leg 1 is required")]
        [RegularExpression("^(PAYER|RECEIVER)$", ErrorMessage = "Direction must be 'PAYER' or 'RECEIVER'")]
        public string DirectionLeg1 { get; set; }

        [JsonProperty("directionLeg2")]
        [Required(ErrorMessage = "Direction Leg 2 is required")]
        [RegularExpression("^(PAYER|RECEIVER)$", ErrorMessage = "Direction must be 'PAYER' or 'RECEIVER'")]
        public string DirectionLeg2 { get; set; }

        [JsonProperty("dividendAmount")]
        public AmountValue DividendAmount { get; set; }

        [JsonProperty("entityStatus")]
        [Required(ErrorMessage = "Entity status is required")]
        [RegularExpression("^(Open|Closed|Pending)$", ErrorMessage = "Entity status must be 'Open', 'Closed' or 'Pending'")]
        public string EntityStatus { get; set; }

        [JsonProperty("initialPrice")]
        public AmountValue InitialPrice { get; set; }

        [JsonProperty("maturity")]
        [Required(ErrorMessage = "Maturity date is required")]
        [FutureDate(ErrorMessage = "Maturity date must be in the future")]
        public DateTime? Maturity { get; set; }

        [JsonProperty("name")]
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
        public string Name { get; set; }

        [JsonProperty("nominal")]
        [Required(ErrorMessage = "Nominal amount is required")]
        public AmountValue Nominal { get; set; }

        [JsonProperty("paymentDatesLeg1")]
        public List<string> PaymentDatesLeg1 { get; set; }

        [JsonProperty("paymentDatesLeg2")]
        public List<string> PaymentDatesLeg2 { get; set; }

        [JsonProperty("spread")]
        public AmountValue Spread { get; set; }

        [JsonProperty("valuationDate")]
        public List<string> ValuationDate { get; set; }

        [JsonProperty("volume")]
        public AmountValue Volume { get; set; }

        [JsonProperty("type")]
        [Required(ErrorMessage = "Type is required")]
        [RegularExpression("^TOTAL_RETURN_SWAP$", ErrorMessage = "Type must be 'TOTAL_RETURN_SWAP'")]
        public string Type { get; set; }
    }
}
