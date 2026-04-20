using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Linq;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("48707CB3-DC75-4ed3-AF8E-576C3632D8E4")]
    public class VolsurfaceSlices : IEnumerable
    {
        IList<VolsurfaceSlice> collection;
        public VolsurfaceSlices(IList<VolsurfaceSlice> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(VolsurfaceSlice item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public VolsurfaceSlice this[int index]
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

    public class FoVolsurfaceSlices : IEnumerable
    {
        readonly IList<FoVolsurfaceSlice> _collection;
        public FoVolsurfaceSlices(IList<FoVolsurfaceSlice> collection)
        {
            _collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return _collection.GetEnumerator();
        }
        public void Add(FoVolsurfaceSlice item)
        {
            _collection.Add(item);
        }
        public void Clear()
        {
            _collection.Clear();
        }
        public FoVolsurfaceSlice this[int index]
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
    [Guid("C90E9175-FCCF-44b4-B729-DD961E72246C")]
    [ComVisible(true)]
    [Serializable]
    public class VolsurfaceSlice
    {
        public int Id { get; set; }
        public String Bucket { get; set; }
        public DateTime Maturity
        {
            get
            {
                try
                {
                    return DateTime.ParseExact(Bucket, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    return new DateTime();
                }
            }
            set
            {
                Bucket = value.ToString("yyyy-MM-dd");
            }
        }

        public double GetDoubleParameter(string name)
        {
            VolsurfaceValue val = Find(name);
            if (val == null)
                throw new PumaMDEException(name + " value not found in this slice");

            return val.Value;
        }
        public string GetStringParameter(string name)
        {
            VolsurfaceValue val = Find(name);
            if (val == null)
                throw new PumaMDEException(name + " value not found in this slice");

            return val.ValueAsString;
        }

        public double[] GetDoubleParametersWithPrefix(string prefix)
        {
            return FindWithPrefix(prefix).OrderBy(x => x.Name).Select(x => x.Value).ToArray();
        }

        public string[] GetStringParametersWithPrefix(string prefix)
        {
            return FindWithPrefix(prefix).OrderBy(x => x.Name).Select(x => x.ValueAsString).ToArray();
        }

        public Volsurface Surface { get; set; }
        public VolsurfaceSlice()
        {
            Values = new List<VolsurfaceValue>();
        }
        public void Add(VolsurfaceValue p)
        {
            p.Slice = this;
            Values.Add(p);
        }
        public void Add(string name, double value)
        {
            VolsurfaceValue val = Find(name);

            if (val != null)
            {
                val.Value = value;
                val.ValueAsString = "";
            }
            else
            {
                val = new VolsurfaceValue();
                val.Slice = this;
                val.Value = value;
                val.ValueAsString = "";
                val.Name = name;
                Values.Add(val);
            }
        }
        public void Add(string name, string value)
        {
            VolsurfaceValue val = Find(name);

            if (val != null)
            {
                val.ValueAsString = value;
                val.Value = 0;
            }
            else
            {
                val = new VolsurfaceValue();
                val.Slice = this;
                val.ValueAsString = value;
                val.Name = name;
                val.Value = 0;
                Values.Add(val);
            }
        }

        [ComVisible(false)]
        public IList<VolsurfaceValue> Values { get; set; }
        public VolsurfaceValues ValuesCollection 
        {
            get
            {
                return new VolsurfaceValues(Values);
            }
        }
        
        VolsurfaceValue Find(string name)
        {
            foreach (VolsurfaceValue v in Values)
            {
                if (v.Name == name)
                    return v;
            }

            return null;
        }

        VolsurfaceValue[] FindWithPrefix(string prefix)
        {
            var retval = new List<VolsurfaceValue>();

            foreach (VolsurfaceValue v in Values)
            {
                if (v.Name.StartsWith(prefix))
                    retval.Add(v);
            }

            return retval.ToArray();
        }

        public VolsurfaceSlice Clone()
        {
            VolsurfaceSlice retval = new VolsurfaceSlice();
            retval.Bucket = Bucket;
            foreach (VolsurfaceValue value in Values)
            {
                retval.Add(value.Clone());
            }
            return retval;
        }
    }

    public class FoVolsurfaceSlice
    {
        public int Id { get; set; }
        public String Bucket { get; set; }
        public DateTime Maturity
        {
            get
            {
                try
                {
                    return DateTime.ParseExact(Bucket, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    return new DateTime();
                }
            }
            set
            {
                Bucket = value.ToString("yyyy-MM-dd");
            }
        }

        public double GetDoubleParameter(string name)
        {
            FoVolsurfaceValue val = Find(name);
            if (val == null)
                throw new PumaMDEException(name + " value not found in this slice");

            return val.Value;
        }
        public string GetStringParameter(string name)
        {
            FoVolsurfaceValue val = Find(name);
            if (val == null)
                throw new PumaMDEException(name + " value not found in this slice");

            return val.ValueAsString;
        }

        public FoVolsurface Surface { get; set; }
        public FoVolsurfaceSlice()
        {
            Values = new List<FoVolsurfaceValue>();
        }
        public void Add(FoVolsurfaceValue p)
        {
            p.Slice = this;
            Values.Add(p);
        }
        public void Add(string name, double value)
        {
            FoVolsurfaceValue val = Find(name);

            if (val != null)
            {
                val.Value = value;
                val.ValueAsString = "";
            }
            else
            {
                val = new FoVolsurfaceValue();
                val.Slice = this;
                val.Value = value;
                val.ValueAsString = "";
                val.Name = name;
                Values.Add(val);
            }
        }
        public void Add(string name, string value)
        {
            FoVolsurfaceValue val = Find(name);

            if (val != null)
            {
                val.ValueAsString = value;
                val.Value = 0;
            }
            else
            {
                val = new FoVolsurfaceValue();
                val.Slice = this;
                val.ValueAsString = value;
                val.Name = name;
                val.Value = 0;
                Values.Add(val);
            }
        }

        [ComVisible(false)]
        public IList<FoVolsurfaceValue> Values { get; set; }
        public FoVolsurfaceValues ValuesCollection 
        {
            get
            {
                return new FoVolsurfaceValues(Values);
            }
        }
        
        FoVolsurfaceValue Find(string name)
        {
            foreach (FoVolsurfaceValue v in Values)
            {
                if (v.Name == name)
                    return v;
            }

            return null;
        }

        public FoVolsurfaceSlice Clone()
        {
            var retval = new FoVolsurfaceSlice();
            retval.Bucket = Bucket;
            foreach (FoVolsurfaceValue value in Values)
            {
                retval.Add(value.Clone());
            }
            return retval;
        }

        public VolsurfaceSlice Copy(Volsurface surface)
        {
            var volsurfaceSlice = new VolsurfaceSlice();
            volsurfaceSlice.Id = Id;
            volsurfaceSlice.Bucket = Bucket;
            volsurfaceSlice.Surface = surface;

            foreach (var value in Values)
            {
                volsurfaceSlice.Values.Add(value.Copy(volsurfaceSlice));
            }
            return volsurfaceSlice;
        }
    }
}