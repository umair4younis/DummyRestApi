using Puma.MDE.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapAccountRuleRow : Entity
    {
        public String DbName { get; set; }
        public int AccountId {  get; set; }
        public int Priority { get; set; }
        public int InstrumentTypeId { get; set; }
        public double MinWeight { get; set; }
        public double MaxWeight { get; set; }
        public double Total { get; set; }
        public double MidWeight { get; set; }
        public string Countries { get; set; }
        public bool IsCountriesNull() { return Countries == null; }

        public string CountriesOrEmpty { get { return (IsCountriesNull()) ? "" : Countries; } }
        public List<string> xCountries
        {
            get
            {
                List<string> list = new List<string>();
                if ((!IsCountriesNull()) && (Countries != string.Empty))
                {
                    string[] codes = Countries.Split(','); // eg. "DE, FR, GB " -> {"DE","FR","GB"}
                    foreach (string code in codes)
                        list.Add(code.Trim());
                }
                return list;
            }
        }

        public String InstrumentTypeName
        {
            get
            {
                SwapAccountInstrumentType instrType = null;
                return instrType != null ? instrType.TypeName : string.Empty;
            }
        }


        // *****

        public double Sum { get; set; }
        public double xTotal { get; set; }
    }
}
