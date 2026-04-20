using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("EDA79DB9-B4AE-45e3-8EC0-D4588ED9CA38")]

    public class Compositions : IEnumerable
    {
        IList<Composition> collection;
        public Compositions(IList<Composition> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(Composition item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public Composition this[int index]
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

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("8BAE51A7-BD24-4fc9-9C9C-243C4861A398")]
    [Serializable]
    public class Composition
    {
        public int Id { get; set; }

        public int ComponentId { get; set; }
        public double Weight { get; set; }

        public Underlying Underlying { get; set; }

        Underlying _component = null;
        public Underlying Component 
        {
            get
            {
                if (_component==null)
                    _component = Engine.Instance.Factory.GetUnderlying(ComponentId);
    
                return _component;
            }
            set
            {
                _component = null;
                try
                {
                    ComponentId = value.Id;
                }
                catch (Exception)
                {
                    ComponentId = 0;
                }
            }
        }
        public string ComponentReference
        {
            get
            {
                try
                {
                    return Component.Reference;
                }
                catch (Exception)
                { 
                }
                return "";
            }
            set
            {
                try
                {
                    Component = Engine.Instance.Factory.GetUnderlying(value);
                }
                catch (Exception)
                {
                    Component = null;
                }
            }
        }

        public Composition Clone()
        {
            Composition retval = new Composition();
            retval.Id = 0;
            retval.Underlying = Underlying;
            retval.Component = Component;
            retval.Weight = Weight;

            return retval;
        }
    }
}
