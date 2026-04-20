using System;

namespace Puma.MDE.Data
{
    public class EuwaxDiscount
    {
        public int      Id                   { get; set; }
        public string   Isin                 { get; set; }
        public string   Wkn                  { get; set; }
        public string   Underlying           { get; set; }
        public string   Issuer               { get; set; }
        public DateTime StartDate            { get; set; }
        public DateTime PremPayDate          { get; set; }
        public DateTime Expiry               { get; set; }
        public DateTime PaymentDate          { get; set; }
        public string   Market               { get; set; }
        public string   Currency             { get; set; }
        public string   ReutersCode          { get; set; }
        public DateTime LastUpdateAtExchange { get; set; }
        public DateTime LastUpdate           { get; set; }
        public DateTime LastChecked          { get; set; }
        public Decimal  IsQuanto             { get; set; }
        public Decimal  Strike               { get; set; }
        public Decimal  Ratio                { get; set; }
        public Decimal  IsEuropean           { get; set; }
        public Decimal  IsCall               { get; set; }
    }
}
