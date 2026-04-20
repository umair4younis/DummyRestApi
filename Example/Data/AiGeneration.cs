using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("8C3B386E-0C07-4cf4-B363-726D968AB1F2")]
    public class AiGenerations : IEnumerable
    {
        IList<AiGeneration> collection;
        public AiGenerations(IList<AiGeneration> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AiGeneration item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AiGeneration this[int index]
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
        public IList<AiGeneration> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("9B23DFD6-1D8E-43ac-93F7-4D02EA8132F7")]
    [ComVisible(true)]
    public class AiGeneration
    {
        public int Id { get; set; }
        public int AlgoId { get; set; }
        public DateTime ValidityDate { get; set; }
        public DateTime DateModif { get; set; }
        public string UserModif { get; set; }
    }
}
