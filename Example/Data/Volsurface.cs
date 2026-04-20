using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;


namespace Puma.MDE.Data
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("C176DE2B-B9D0-42a6-82D4-E3A93E3B776D")]
    public class Volsurfaces : IEnumerable
    {
        IList<Volsurface> collection;
        public Volsurfaces(IList<Volsurface> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(Volsurface item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public Volsurface this[int index]
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
        [ComVisible(false)]
        public IList<Volsurface> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    public class FoVolsurfaces : IEnumerable
    {
        readonly IList<FoVolsurface> _collection;
        public FoVolsurfaces(IList<FoVolsurface> collection)
        {
            this._collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return _collection.GetEnumerator();
        }
        public void Add(FoVolsurface item)
        {
            _collection.Add(item);
        }
        public void Clear()
        {
            _collection.Clear();
        }
        public FoVolsurface this[int index]
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
        [ComVisible(false)]
        public IList<FoVolsurface> Collection
        {
            get
            {
                return _collection;
            }
        }
    }

   
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("06BC4D9B-E448-4ce3-A7D0-B887F1BB2E7B")]
    [ComVisible(true)]
    [Serializable]
    public class Volsurface
    {
        public int Id { get; set; }
        public int UnderlyingId { get; set; }
        public int VolmodelId { get; set; }
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public int UsedMarketdataId { get; set; }
        public bool PublishedToSophis { get; set; }

        public VolsurfaceSlice GetVolsurfaceSlice(DateTime maturity)
        {
            if (Slices == null || Slices.Count == 0)
                return null;

            // This wont throw exception if there none found, just return null
            return Slices.FirstOrDefault((slice) => (slice.Maturity == maturity));
        }
        public Volsurface()
        {
            Slices = new List<VolsurfaceSlice>();

            User = Engine.Instance.ConnectedUser;
            Timestamp = DateTime.Now;
            PublishedToSophis = false;
        }
        public void Add(VolsurfaceSlice p)
        {
            p.Surface = this;
            Slices.Add(p);
        }

        [ComVisible(false)]
        public IList<VolsurfaceSlice> Slices { get; set; }

        public VolsurfaceSlices SlicesCollection 
        { 
            get
            {
                return new VolsurfaceSlices(Slices.OrderBy( x => x.Maturity ).ToList());
            }
        }

        public User User
        {
            get
            {
                return Engine.Instance.Factory.GetUser(UserId);
            }
            set
            {
                UserId = value.Id;
            }
        }
        public Underlying Underlying
        {
            get
            {
                return Engine.Instance.Factory.GetUnderlying(UnderlyingId);
            }
            set
            {
                UnderlyingId = value.Id;
            }
        }
        public UsedMarketdata UsedMarketdata
        {
            get
            {
                return Engine.Instance.Factory.GetUsedMarketdata(UsedMarketdataId);
            }
            set
            {
                UsedMarketdataId = value.Id;
            }
        }
        public VolsurfaceModel VolsurfaceModel
        {
            get
            {
                return Engine.Instance.Factory.GetVolsurfaceModel(VolmodelId);
            }
            set
            {
                VolmodelId = value.Id;
            }
        }

        public double[] GetSlicedValues(string name)
        {
            List<double> retval = new List<double>();
            foreach (VolsurfaceSlice slice in Slices.OrderBy(x => x.Maturity))
            {
                retval.Add(slice.GetDoubleParameter(name));
            }
            return retval.ToArray();
        }

        public DateTime[] GetSlicedMaturities()
        {
            List<DateTime> retval = new List<DateTime>();
            foreach (VolsurfaceSlice slice in Slices)
            {
                retval.Add(slice.Maturity);
            }
            retval.Sort();
            return retval.ToArray();
        }

        public Dictionary<DateTime, double[]> GetSlicedValuesWithPrefix(string prefix)
        {
            return Slices.ToDictionary(x => x.Maturity, x => x.GetDoubleParametersWithPrefix(prefix));
        }

        public void ConstructEmptySurface(DateTime[] maturities)
        {
            foreach (DateTime mat in maturities)
            {
                VolsurfaceSlice slice = new VolsurfaceSlice();
                
                slice.Maturity = mat;
                foreach (VolsurfaceModelParameter p in VolsurfaceModel.Parameters)
                {
                    slice.Add(p.VariableName, 0);
                }
                Add(slice);
            }
        }
        static public Volsurface CreateInstance(VolsurfaceModel model, Underlying und)
        {
            Volsurface instance = new Volsurface();

            instance.Underlying = und;
            instance.Timestamp = DateTime.Now;
            instance.VolsurfaceModel = model;
            instance.User = Engine.Instance.ConnectedUser;

            return instance;

        }

        public Volsurface Clone()
        {
            Volsurface retval = CreateInstance(VolsurfaceModel, Underlying);
            foreach (VolsurfaceSlice slice in Slices)
            {
                retval.Add(slice.Clone());
            }
            return retval;
        }

    }

    public class FoVolsurface
    {
        public int Id { get; set; }
        public int UnderlyingId { get; set; }
        public int VolmodelId { get; set; }
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public int UsedMarketdataId { get; set; }
        public bool PublishedToSophis { get; set; }

        public FoVolsurfaceSlice GetVolsurfaceSlice(DateTime maturity)
        {
            return Slices.First((slice) => (slice.Maturity == maturity));
        }
        public FoVolsurface()
        {
            Slices = new List<FoVolsurfaceSlice>();

            User = Engine.Instance.ConnectedUser;
            Timestamp = DateTime.Now;
            PublishedToSophis = false;
        }
        public void Add(FoVolsurfaceSlice p)
        {
            p.Surface = this;
            Slices.Add(p);
        }

        [ComVisible(false)]
        public IList<FoVolsurfaceSlice> Slices { get; set; }

        public FoVolsurfaceSlices SlicesCollection 
        { 
            get
            {
                return new FoVolsurfaceSlices(Slices.OrderBy( x => x.Maturity ).ToList());
            }
        }

        public User User
        {
            get
            {
                return Engine.Instance.Factory.GetUser(UserId);
            }
            set
            {
                UserId = value.Id;
            }
        }
        public Underlying Underlying
        {
            get
            {
                return Engine.Instance.Factory.GetUnderlying(UnderlyingId);
            }
            set
            {
                UnderlyingId = value.Id;
            }
        }
        public UsedMarketdata UsedMarketdata
        {
            get
            {
                return Engine.Instance.Factory.GetUsedMarketdata(UsedMarketdataId);
            }
            set
            {
                UsedMarketdataId = value.Id;
            }
        }
        public FoVolsurfaceModel VolsurfaceModel
        {
            get
            {
                return null;
                
            }
            set
            {
                VolmodelId = value.Id;
            }
        }

        public double[] GetSlicedValues(string name)
        {
            var retval = new List<double>();
            foreach (FoVolsurfaceSlice slice in Slices.OrderBy(x => x.Maturity))
            {
                retval.Add(slice.GetDoubleParameter(name));
            }
            return retval.ToArray();
        }

        public DateTime[] GetSlicedMaturities()
        {
            var retval = new List<DateTime>();
            foreach (FoVolsurfaceSlice slice in Slices)
            {
                retval.Add(slice.Maturity);
            }
            retval.Sort();
            return retval.ToArray();
        }

        public void ConstructEmptySurface(DateTime[] maturities)
        {
            foreach (DateTime mat in maturities)
            {
                var slice = new FoVolsurfaceSlice();
                
                slice.Maturity = mat;
                foreach (FoVolsurfaceModelParameter p in VolsurfaceModel.Parameters)
                {
                    slice.Add(p.VariableName, 0);
                }
                Add(slice);
            }
        }
        static public FoVolsurface CreateInstance(FoVolsurfaceModel model, Underlying und)
        {
            var instance = new FoVolsurface();

            instance.Underlying = und;
            instance.Timestamp = DateTime.Now;
            instance.VolsurfaceModel = model;
            instance.User = Engine.Instance.ConnectedUser;

            return instance;

        }

        public FoVolsurface Clone()
        {
            FoVolsurface retval = CreateInstance(VolsurfaceModel, Underlying);
            foreach (FoVolsurfaceSlice slice in Slices)
            {
                retval.Add(slice.Clone());
            }
            return retval;
        }

        public Volsurface Copy()
        {
            var volSurface = new Volsurface();
            volSurface.Id = Id;
            volSurface.UnderlyingId = UnderlyingId;
            volSurface.VolmodelId = VolmodelId;
            volSurface.UserId = UserId;
            volSurface.Timestamp = Timestamp;
            volSurface.UsedMarketdataId = UsedMarketdataId;
            volSurface.PublishedToSophis = PublishedToSophis;
           
            foreach (var slice in Slices)
            {
                volSurface.Slices.Add(slice.Copy(volSurface));
            }

            return volSurface;
        }
    }

}