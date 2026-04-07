using System;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    /// <summary>
    /// PATCH DTO – all fields are optional (nullable by default).
    /// Use NullValueHandling.Ignore to exclude null properties from JSON.
    /// </summary>
    public class SwapAccountValuationPatch
    {
        [JsonProperty("accountSegment", NullValueHandling = NullValueHandling.Ignore)]
        public string AccountSegment { get; set; }

        [JsonProperty("asset", NullValueHandling = NullValueHandling.Ignore)]
        public string Asset { get; set; }

        [JsonProperty("assetClassLiteral", NullValueHandling = NullValueHandling.Ignore)]
        public string AssetClassLiteral { get; set; }

        [JsonProperty("assetTreeTypes", NullValueHandling = NullValueHandling.Ignore)]
        public AssetTreeTypes AssetTreeTypes { get; set; }

        [JsonProperty("businessHolidayFixingLeg1", NullValueHandling = NullValueHandling.Ignore)]
        public string BusinessHolidayFixingLeg1 { get; set; }

        [JsonProperty("businessHolidayFixingLeg2", NullValueHandling = NullValueHandling.Ignore)]
        public string BusinessHolidayFixingLeg2 { get; set; }

        [JsonProperty("counterpart", NullValueHandling = NullValueHandling.Ignore)]
        public string Counterpart { get; set; }

        [JsonProperty("currency", NullValueHandling = NullValueHandling.Ignore)]
        public string Currency { get; set; }

        [JsonProperty("dayCountConvention", NullValueHandling = NullValueHandling.Ignore)]
        public string DayCountConvention { get; set; }

        [JsonProperty("designatedMaturity", NullValueHandling = NullValueHandling.Ignore)]
        public string DesignatedMaturity { get; set; }

        [JsonProperty("directionLeg1", NullValueHandling = NullValueHandling.Ignore)]
        public string DirectionLeg1 { get; set; }

        [JsonProperty("directionLeg2", NullValueHandling = NullValueHandling.Ignore)]
        public string DirectionLeg2 { get; set; }

        [JsonProperty("dividendAmount", NullValueHandling = NullValueHandling.Ignore)]
        public AmountValue DividendAmount { get; set; }

        [JsonProperty("entityStatus", NullValueHandling = NullValueHandling.Ignore)]
        public string EntityStatus { get; set; }

        [JsonProperty("initialPrice", NullValueHandling = NullValueHandling.Ignore)]
        public AmountValue InitialPrice { get; set; }

        [JsonProperty("maturity", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Maturity { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("nominal", NullValueHandling = NullValueHandling.Ignore)]
        public AmountValue Nominal { get; set; }

        [JsonProperty("paymentDatesLeg1", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> PaymentDatesLeg1 { get; set; }

        [JsonProperty("paymentDatesLeg2", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> PaymentDatesLeg2 { get; set; }

        [JsonProperty("spread", NullValueHandling = NullValueHandling.Ignore)]
        public AmountValue Spread { get; set; }

        [JsonProperty("valuationDate", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ValuationDate { get; set; }

        [JsonProperty("volume", NullValueHandling = NullValueHandling.Ignore)]
        public AmountValue Volume { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
    }
}