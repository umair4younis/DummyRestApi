using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapAccountInstrumentType : Entity
    {
        public SwapAccountInstrumentType() { }
        public SwapAccountInstrumentType(int id)
        {
            this.Id = id;
        }
        public String TypeName { get; set; }

        public bool IsCash { get { return this.TypeName == "Cash"; } }
        public bool IsCashFee { get { return this.TypeName == "CashFee"; } }
        public bool IsBond { get { return this.TypeName == "Bond"; } }
        public bool IsStock { get { return this.TypeName == "Stock"; } }
        public bool IsFund { get { return this.TypeName == "Fund"; } }
        public bool IsFuture { get { return this.TypeName == "Future"; } }
        public bool IsRate { get { return this.TypeName == "Rate"; } }
        public bool IsFXRate { get { return this.TypeName == "FXRate"; } }
        public bool IsETF { get { return this.TypeName == "ETF"; } }
        public bool IsInventory { get { return this.TypeName == "Inventory"; } }
        public bool IsFeedInstrument { get { return this.TypeName == "Feed Instrument"; } }
        public bool IsFXFwd { get { return this.TypeName == "FXFwd"; } }
        public bool IsFXOpt { get { return this.TypeName == "FXOpt"; } }
        public bool IsFXSpread { get { return this.TypeName == "FXSpread"; } }
        public bool IsFXDerivative { get { return IsFXOpt || IsFXFwd || IsFXSpread; } }
        public bool IsEquityOption { get { return this.TypeName == "EquityOption"; } }
        public bool IsFutureCash { get { return this.TypeName == "FutureCash"; } }
        public bool IsFXSpot { get { return this.TypeName == "FXSpot"; } }
        public bool IsPreciousMetal { get { return this.TypeName == "PreciousMetal"; } }
        public bool IsCertificate { get { return this.TypeName == "Certificate"; } }
        public bool IsADR { get { return this.TypeName == "ADR"; } }
        public bool IsGDR { get { return this.TypeName == "GDR"; } }
        public bool IsIndex { get { return this.TypeName == "Index"; } }
        public bool IsCommission { get { return this.TypeName == "Commission"; } }

        public SwapAccountInstrumentType CloneWithoutIds()
        {
            var clone = this.Clone<SwapAccountInstrumentType>();
            clone.Id = 0;
            clone.TypeName = this.TypeName;
            return clone;
        }
    }

}
