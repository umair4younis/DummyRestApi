using System;

namespace Puma.MDE.Data
{
    public class EuwaxBonus
    {
        public int      Id                      { get; set; }
        public string   Isin                    { get; set; }
        public string   Wkn                     { get; set; }
        public string   Underlying              { get; set; }
        public string   Issuer                  { get; set; }
        public DateTime StartDate               { get; set; }
        public DateTime PremPayDate             { get; set; }
        public DateTime Expiry                  { get; set; }
        public DateTime PaymentDate             { get; set; }
        public string   Market                  { get; set; }
        public string   Currency                { get; set; }
        public string   ReutersCode             { get; set; }
        public DateTime LastUpdateAtExchange    { get; set; }
        public DateTime LastUpdate              { get; set; }
        public DateTime LastChecked             { get; set; }
        public Decimal  IsQuanto                { get; set; }

        // properties specific to EUWX_BONUS
        public Decimal  Barrier                 { get; set; }
        public Decimal  Bonus                   { get; set; }
        public Decimal  Cap                     { get; set; }
        public Decimal  Ratio                   { get; set; }
        public Decimal  BarrierHit              { get; set; }
        public DateTime BarrierStartDate        { get; set; }
        public DateTime BarrierEndDate          { get; set; }
        public Decimal  Continuous              { get; set; }
    }
}
