using System;

namespace Puma.MDE.Data
{
    public class EuwaxUnderlying
    {
        public int      Id          { get; set; }
        public string   Ric         { get; set; }
        public string   Isin        { get; set; }
        public string   Wkn         { get; set; }
        public string   BbId        { get; set; }
        public string   Name        { get; set; }
        public string   Country     { get; set; }
        public string   Currency    { get; set; }
        public string   Sector      { get; set; }
        public DateTime NextErDate  { get; set; }
        public Decimal  LastErMove  { get; set; }
        public Decimal  Last2ErMove { get; set; }
        public Decimal  Last4ErMove { get; set; }
        public Decimal  Last6ErMove { get; set; }
        public Decimal  Last8ErMove { get; set; }
        public DateTime UpdateDate  { get; set; }
    }
}
