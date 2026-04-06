using Newtonsoft.Json;
using System;
using System.Collections.Generic;


namespace Example.OPUS.Models
{
    /// <summary>
    /// Full response model for a Total Return Swap from OPUS API.
    /// Matches the exact JSON structure provided (flat object, no pagination/Data wrapper).
    /// </summary>
    public class TotalReturnSwapResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("accountSegment")]
        public string AccountSegment { get; set; }

        [JsonProperty("asset")]
        public string Asset { get; set; }

        [JsonProperty("assetAtMarketplaces")]
        public List<AssetAtMarketplaceDetail> AssetAtMarketplaces { get; set; }

        [JsonProperty("assetClassLiteral")]
        public string AssetClassLiteral { get; set; }

        [JsonProperty("assetTreeTypes")]
        public AssetTreeTypes AssetTreeTypes { get; set; }

        [JsonProperty("Bloomberg Reference Query")]
        public string BloombergReferenceQuery { get; set; }

        [JsonProperty("businessHolidayFixingLeg1")]
        public string BusinessHolidayFixingLeg1 { get; set; }

        [JsonProperty("businessHolidayFixingLeg2")]
        public string BusinessHolidayFixingLeg2 { get; set; }

        [JsonProperty("corporateActions")]
        public List<object> CorporateActions { get; set; }

        [JsonProperty("counterpart")]
        public object Counterpart { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("creationUser")]
        public string CreationUser { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("dayCountConvention")]
        public string DayCountConvention { get; set; }

        [JsonProperty("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        [JsonProperty("deletedBy")]
        public string DeletedBy { get; set; }

        [JsonProperty("designatedMaturity")]
        public string DesignatedMaturity { get; set; }

        [JsonProperty("directionLeg1")]
        public string DirectionLeg1 { get; set; }

        [JsonProperty("directionLeg2")]
        public string DirectionLeg2 { get; set; }

        [JsonProperty("dividendAmount")]
        public AmountValue DividendAmount { get; set; }

        [JsonProperty("entityStatus")]
        public string EntityStatus { get; set; }

        [JsonProperty("exchange")]
        public object Exchange { get; set; }

        [JsonProperty("Exchange Asset Identifier")]
        public object ExchangeAssetIdentifier { get; set; }

        [JsonProperty("fixedRate")]
        public object FixedRate { get; set; }

        [JsonProperty("floatingRateReferenceIndex")]
        public object FloatingRateReferenceIndex { get; set; }

        [JsonProperty("initialPrice")]
        public AmountValue InitialPrice { get; set; }

        [JsonProperty("ISIN")]
        public string Isin { get; set; }

        [JsonProperty("manuallyUpdatedAt")]
        public DateTime? ManuallyUpdatedAt { get; set; }

        [JsonProperty("manuallyUpdateUser")]
        public string ManuallyUpdateUser { get; set; }

        [JsonProperty("maturity")]
        public string Maturity { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("nominal")]
        public AmountValue Nominal { get; set; }

        [JsonProperty("paymentDatesLeg1")]
        public List<string> PaymentDatesLeg1 { get; set; }

        [JsonProperty("paymentDatesLeg2")]
        public List<string> PaymentDatesLeg2 { get; set; }

        [JsonProperty("physicalDelivery")]
        public object PhysicalDelivery { get; set; }

        [JsonProperty("reconciliationLeg")]
        public object ReconciliationLeg { get; set; }

        [JsonProperty("Reuters Reference Query")]
        public object ReutersReferenceQuery { get; set; }

        [JsonProperty("rolledOverBy")]
        public object RolledOverBy { get; set; }

        [JsonProperty("rollOver")]
        public object RollOver { get; set; }

        [JsonProperty("spread")]
        public AmountValue Spread { get; set; }

        [JsonProperty("tradedOnEeaVenue")]
        public object TradedOnEeaVenue { get; set; }

        [JsonProperty("transactionReporting")]
        public object TransactionReporting { get; set; }

        [JsonProperty("UC_ID")]
        public object UcId { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("updateUser")]
        public string UpdateUser { get; set; }

        [JsonProperty("valuationDate")]
        public List<string> ValuationDate { get; set; }

        [JsonProperty("verifiedAt")]
        public DateTime? VerifiedAt { get; set; }

        [JsonProperty("verifyUser")]
        public string VerifyUser { get; set; }

        [JsonProperty("volume")]
        public AmountValue Volume { get; set; }

        [JsonProperty("WKN")]
        public object Wkn { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}