using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapPool : Entity
    {
        public SwapPool() { this.Id = 1; }
        public SwapPool(int id) { this.Id = id; }
        public SwapPool(SwapAccountInstrument instr, double nominal, int rowVersion)
        {
            this.DbName = instr.DbName;
            this.InstrumentId = instr.Id;
            this.Nominal = nominal;
            this.RowVersion = rowVersion;
        }

        public SwapPool DeepCopy() => new SwapPool(Id)
        {
            DbName = DbName,
            InstrumentId = InstrumentId,
            Nominal = Nominal,
            RowVersion = RowVersion,
            Price = Price,
            FxRate = FxRate,
            OldNominal = OldNominal
        };

        public String DbName { get; set; }
        public int InstrumentId { get; set; }
        public double Nominal { get; set; }
        public int RowVersion { get; set; }

        // *****
        public void SetGridProperties(double price, double fxRate)
        {
            Price = price;
            FxRate = fxRate;
        }

        public SwapAccountInstrument getInstrument()
        {
            return new SwapAccountInstrument
            {

            };
        }

        public string xExDivDate
        {
            get
            {
                SwapAccountInstrument instr = getInstrument();
                if (((instr.InstrumentType.IsStock) || (instr.InstrumentType.IsETF)) && (!instr.IsISINNull()))
                {
                    DateTime date = new DateTime();
                    return date.ToString("dd/MM/yyyy");
                }
                return string.Empty;
            }
        }

        public double Price { get; set; }
        public double FxRate { get; set; }
        public double PriceEUR { get { return Price * FxRate; } }
        public double VolumeEUR { get { return Nominal * PriceEUR; } }
        public double Weight(double total) { return VolumeEUR / total; }
        public double OldNominal { get; set; } // nominal in basket before the reshuffle
        public double OldVolumeEUR { get { return OldNominal * PriceEUR; } }
        public double OldWeight(double total) { return OldVolumeEUR / total; }
        public bool InBasket
        {
            get { return OldNominal > 0.0; }
        }
    }
}
