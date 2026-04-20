using System;

namespace Puma.MDE.Data
{
    public class HVBUnderlyingShiftClasses
    {
        public HVBUnderlyingShiftClasses()
        {
            MyOneMarketsValues = null;
            DeritradeSUValues = null;
            DeritradeMUValues = null;
            BoApprovedValues = null;
        }
        public int Sicovam { get; set; }
        public int ShiftClassId { get; set; }
        public int  UnderlyingTypeId { get; set; }
        public string Client1 { get; set; }
        public string Client2 { get; set; }
        public string IsMyOneMarket { get; set; }
        public HVBMassShiftClass HvbMassShiftClass { get; set; }
        public HVBUnderlyingTypes HvbMassUnderlyingTypes { get; set; }
        public HVBTitresUnderShiftClass HvbTitresUnderShiftClass { get; set; }
        public string ApprovedByFO { get; set; }
        public string ApprovedByBO { get; set; }
        public string ApprovedByFOMulti { get; set; }
        public double IndicativeInterval { get; set; }
        public double TradeableInterval { get; set; }
        public object MyOneMarketsValues { get; set; }
        public object DeritradeSUValues { get; set; }
        public object DeritradeMUValues { get; set; }
        public object BoApprovedValues { get; set; }
        public string ReferenceDisplayValue { get; set; }
        public string ShitClassDisplayValue { get; set; }
        public String UnderlyingTypeDisplayValue { get; set; }
        public string IsSubscription { get; set; }
        public string PDBlacklist { get; set; }
        public string PDBlacklistFull { get; set; }
        public bool IsDirty { get; set; }
    }
}
