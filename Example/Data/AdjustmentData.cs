using System;

namespace Puma.MDE.Data
{
    public class AdjustmentData
    {
        public int      Refcon                      { get; set; }
        public int      SophisInstrumentId          { get; set; }
        public DateTime AdjustmentDate              { get; set; }
        public Decimal  RFactor                     { get; set; }
        public String   Comment                     { get; set; }
        public int      AdjustmentType              { get; set; }
        public DateTime DividendPaymentDate         { get; set; }
        public Decimal  MarketFees                  { get; set; }
        public int      StockReference              { get; set; }
        public int      DividendCurrency            { get; set; }
        public Decimal  ExchangeRate                { get; set; }
        public Decimal  Cash                        { get; set; }
        public int      CashCurrency                { get; set; }
        public int      ConversionRatioNumerator    { get; set; }
        public int      ConversionRatioDenominator  { get; set; }
        public int      RoundingMethod              { get; set; }
        public int      FirstBusinessEvent          { get; set; }
        public int      SecondBusinessEvent         { get; set; }
        public DateTime ExDividendDate              { get; set; }
        public Decimal  CounterpartyFees            { get; set; }
        public Decimal  Commission                  { get; set; }
        public DateTime CommissionDate              { get; set; }
        public DateTime LastExecutionDate           { get; set; }
        public int      LastExecutionUserId         { get; set; }
        public int      LastExecutionVersion        { get; set; }
        public int      CorporateActionType         { get; set; }
    }
}
