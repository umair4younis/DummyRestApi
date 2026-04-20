using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;


namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapPriceRule : Entity
    {
        public String DbName { get; set; }
        public int AccountId { get; set; }
        public int Priority {  get; set; }
        public int? InstrumentTypeId { get; set; }
        public int? RegionId {  get; set; }
        public int RefDateId { get; set; }
        public int PriceFieldId { get; set; }
        public DateTime SpecificDate {  get; set; }

    }
}
