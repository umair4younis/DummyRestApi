using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("2696C654-32CB-4b71-870B-6EEC58F87915")]
    [ComVisible(true)]

    public class UsedMarketdata
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public String Context { get; set; }
        public UsedMarketdata()
        {
            Values = new List<UsedMarketdataValue>();
        }

        public UsedMarketdata Clone()
        {
            UsedMarketdata retval = new UsedMarketdata() 
            {
                Timestamp = Timestamp, 
                Context = Context
            };

            foreach (UsedMarketdataValue value in Values)
            {
                retval.Add(value.Clone());
            }
            return retval;
        }

        public void Add(UsedMarketdataValue p)
        {
            p.Data = this;
            Values.Add(p);
        }
        public void Add(string name, double value)
        {
            UsedMarketdataValue p = new UsedMarketdataValue();

            p.Name = name;
            p.Value = value;
            p.ValueAsString = "";
            Add(p);
        }

        [ComVisible(false)]
        public IList<UsedMarketdataValue> Values { get; set; }

        public UsedMarketdataValues ValuesCollection
        {
            get
            {
                return new UsedMarketdataValues(Values);
            }
        }
    }

}
