using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("A1E40EBB-DAA8-42d4-B050-F8BA3BDC752F")]
    public class VolsurfaceModelParameters : IEnumerable
    {
        IList<VolsurfaceModelParameter> collection;
        public VolsurfaceModelParameters(IList<VolsurfaceModelParameter> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(VolsurfaceModelParameter item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public VolsurfaceModelParameter this[int index]
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

    public class FoVolsurfaceModelParameters : IEnumerable
    {
        readonly IList<FoVolsurfaceModelParameter> _collection;
        public FoVolsurfaceModelParameters(IList<FoVolsurfaceModelParameter> collection)
        {
            this._collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return _collection.GetEnumerator();
        }
        public void Add(FoVolsurfaceModelParameter item)
        {
            _collection.Add(item);
        }
        public void Clear()
        {
            _collection.Clear();
        }
        public FoVolsurfaceModelParameter this[int index]
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
    [Guid("EC591375-8850-4c38-B5B8-1254B005DBA8")]
    [ComVisible(true)]

    public class VolsurfaceModelParameter
    {
        public int Id {get;set;}
        public String VariableName { get; set; }
        public int OrderRank { get; set; }

        public VolsurfaceModelParameter(String varname, VolsurfaceModel model)
        {
            VariableName = varname;
            Model = model ;
        }
        public VolsurfaceModelParameter(String name)
        {
            VariableName = name;
        }
        public VolsurfaceModelParameter()
        {
        }
        public VolsurfaceModel Model { get; set; }

        public bool Extrapolable {get; set;}
        public bool Floorable {get; set;}
    
    }

    public class FoVolsurfaceModelParameter
    {
        public int Id {get;set;}
        public String VariableName { get; set; }
        public int OrderRank { get; set; }

        public FoVolsurfaceModelParameter(String varname, FoVolsurfaceModel model)
        {
            VariableName = varname;
            Model = model ;
        }
        public FoVolsurfaceModelParameter(String name)
        {
            VariableName = name;
        }
        public FoVolsurfaceModelParameter()
        {
        }
        public FoVolsurfaceModel Model { get; set; }

        public bool Extrapolable {get; set;}
        public bool Floorable {get; set;}

        public VolsurfaceModelParameter Copy(VolsurfaceModel model)
         {
             var volsurfaceModelParameter = new VolsurfaceModelParameter();
             volsurfaceModelParameter.Id = Id;
             volsurfaceModelParameter.VariableName = VariableName;
             volsurfaceModelParameter.Model = model;

             return volsurfaceModelParameter;
         }
    }

}