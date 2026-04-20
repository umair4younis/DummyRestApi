using System;

using System.Runtime.InteropServices;
using System.Globalization;

namespace Puma.MDE.Data
{
    [ComVisible(false)]
    public class VarSwapParameters
    {
        public VarSwapParameters()
        {
            CashDivAdjCutoff = 1;
            StrikeStep =1 ;
            AlphaS = 0;
            AlphaL = 0;
            AddOnModel1 = 0;
            AddOnModel2 = 0;
        }

        public long Id { get; set; }
        
        public int UnderlyingId { get; set; }
        public string Maturity { get; set; }

        public DateTime Expiry
        {
            get
            {
                try
                {
                    return DateTime.ParseExact(Maturity, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    return new DateTime();
                }
            }
            set
            {
                Maturity = value.ToString("yyyy-MM-dd");
            }
        }

        public double AddOnModel1 { get; set; }
        public double AddOnModel2 { get; set; }

        public double CashDivAdjCutoff { get; set; }
        public double StrikeStep { get; set; }
        public double AlphaS { get; set; }
        public double AlphaL { get; set; }

    }
}
