using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("BD6035FE-91AA-407e-8DF6-45D2667AB063")]
    public class VolsurfaceValues : IEnumerable
    {
        IList<VolsurfaceValue> collection;
        public VolsurfaceValues(IList<VolsurfaceValue> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(VolsurfaceValue item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public VolsurfaceValue this[int index]
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

    public class FoVolsurfaceValues : IEnumerable
    {
        readonly IList<FoVolsurfaceValue> _collection;
        public FoVolsurfaceValues(IList<FoVolsurfaceValue> collection)
        {
            _collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return _collection.GetEnumerator();
        }
        public void Add(FoVolsurfaceValue item)
        {
            _collection.Add(item);
        }
        public void Clear()
        {
            _collection.Clear();
        }
        public FoVolsurfaceValue this[int index]
        {
            get
            {
                return _collection[index];
            }
            set
            {
                _collection[index] = value;
            }
        }
        public int Count
        {
            get
            {
                return _collection.Count;
            }
        }
    }


    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("D306D990-EFFF-4e34-AE78-9984D3F9333F")]
    [ComVisible(true)]
    [Serializable]
    public class VolsurfaceValue
    {
        public long Id { get; set; }
        public String Name { get; set; }
        public double Value { get; set; }
        public String ValueAsString { get; set; }

        public VolsurfaceSlice Slice { get; set; }
        public VolsurfaceValue Clone()
        {
            var retval = new VolsurfaceValue
            {
                 Name = Name, 
                 Value = Value, 
                 ValueAsString = ValueAsString
            };

            return retval;
        }
    }

    public class FoVolsurfaceValue
    {
        public long Id { get; set; }
        public String Name { get; set; }
        public double Value { get; set; }
        public String ValueAsString { get; set; }

        public FoVolsurfaceSlice Slice { get; set; }

        public FoVolsurfaceValue Clone()
        {
            var retval = new FoVolsurfaceValue
            {
                Name = Name, 
                Value = Value, 
                ValueAsString = ValueAsString
            };

            return retval;
        }

        public VolsurfaceValue Copy(VolsurfaceSlice slice)
        {
            var volsurfaceValue = new VolsurfaceValue();
            volsurfaceValue.Id = Id;
            volsurfaceValue.Name = Name;
            volsurfaceValue.Value = Value;
            volsurfaceValue.ValueAsString = ValueAsString;
            volsurfaceValue.Slice = slice;

            return volsurfaceValue;
        }
    }

}