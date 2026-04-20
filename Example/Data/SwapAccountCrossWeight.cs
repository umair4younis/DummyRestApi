using Puma.MDE.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{


    [ComVisible(true)]
    [Serializable]
    public class SwapAccountCrossWeight : Entity
    {
        public String DbName { get; set; }
        public int? AccountId { get; set; }
        public String InstrumentTypes { get; set; }

        public double? MinWeight { get; set; }
        public double? Weight { get; set; }
        public int AlertColor { get; set; }


        public List<int> GetInstrumentTypeIds(out string error)
        {
            List<int> ids = new List<int>();
            error = "";

            if (string.IsNullOrEmpty(InstrumentTypes))
                return ids;

            string[] names = InstrumentTypes.Split(";,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries); // eg. string "Stock,Certificate;Fund" gives array {"Stock","Certificate","Fund"}
            foreach (string n in names)
            {
                var type = n.Trim();
                if (type == null)
                {
                    error = string.Format("unknown instrument type '{0}'", n.Trim());
                    return ids;
                }
                ids.Add(1);
            }
            ids.Sort();
            return ids;
        }

        public int xAlertColor
        {
            get
            {
                return AlertColor;
            }
            set
            {
                AlertColor = value;
                NotifyPropertyChanged(() => AlertColor);
                NotifyPropertyChanged(() => xAlertColor);
                NotifyPropertyChanged(() => AlertBrush);
            }
        }

        public object AlertBrush
        {
            get
            {
                return null;
            }
            
        }
    }
}
