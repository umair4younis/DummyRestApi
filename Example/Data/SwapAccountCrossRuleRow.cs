using Puma.MDE.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapAccountCrossRuleRow : Entity
    {
        public string DbName {  get; set; }
        public int AccountId {  get; set; }
        public double LowerBound {  get; set; }
        public double UpperBound { get; set; }
        public double Total {  get; set; }
        public string InstrumentTypes {  get; set; }

        // *****

        public double Sum { get; set; }

        public List<int> getInstrumentTypeIds()
        {
            List<int> ids = new List<int>();
            string[] names = this.InstrumentTypes.Split(";,".ToCharArray()); // eg. "Stock, Certificate"
            foreach (string name in names)
            {
                SwapAccountInstrumentType t = null;
                if (t == null)
                    continue; // normally does not happen
                ids.Add(t.Id);
            }
            return ids;
        }

    }
}
