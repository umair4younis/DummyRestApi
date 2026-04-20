using System;
using System.Collections;
using System.Collections.Generic;

namespace Puma.MDE.Data
{
    public class BasketFixingConfigurations : IEnumerable
    {
        IList<BasketFixingConfiguration> collection;
        public BasketFixingConfigurations(IList<BasketFixingConfiguration> collection)
        {
            this.collection = collection;
        }
        
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(BasketFixingConfiguration item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public BasketFixingConfiguration this[int index]
        {
            get
            {
                return collection[index];
            }
            set
            {
                collection[index] = value;
            }
        }
        public int Count
        {
            get
            {
                return collection.Count;
            }
        }
    }
    [Serializable]
    public class BasketFixingConfiguration
    {
        public int Id { get; set; }

        public Underlying Underlying { get; set; }

        public int ComponentCode { get; set; }
        public string ComponentReference { get; set; }
        public string ComponentColumn { get; set; }

        public int FxCode { get; set; }
        public string FxReference { get; set; }
        public string FxColumn { get; set; }

        public BasketFixingConfiguration Clone()
        {
            BasketFixingConfiguration retval = new BasketFixingConfiguration();
            retval.Id = 0;
            retval.Underlying = Underlying;
            retval.ComponentCode = ComponentCode;
            retval.ComponentReference = ComponentReference;
            retval.ComponentColumn = ComponentColumn;
            retval.FxCode = FxCode;
            retval.FxReference = FxReference;
            retval.FxColumn = FxColumn;

            return retval;
        }
    }
}
