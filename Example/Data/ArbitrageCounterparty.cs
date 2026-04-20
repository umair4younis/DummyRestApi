using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Runtime.InteropServices;

// Trac: #37794 - MDE: Add SmartOrc Project for Auomatic Quotation Updates
namespace Puma.MDE.Data
{
    // data from: select * from puma_mde_arbitrage_cparty
    public class ArbitrageCounterparty
    {
        public int      Id                 { get; set; }
        public string   Counterparty       { get; set; }
        public DateTime StartDate          { get; set; }
        public DateTime EndDate            { get; set; }
        public String   ClassificationName { get; set; }
        public int      ClassificationId   { get; set; }
        public bool     Enabled            { get; set; }

        // parameters for handling adding, removing or updating data in the grid
        public bool     IsRowUpdated       { get; set; }
        public bool     IsRowDeleted       { get; set; }
        public bool     IsRowNew           { get; set; }

        // container for the ArbitrageCounterpartyExcludedProduct(s)
        [ComVisible(false)]
        public IList<ArbitrageCounterpartyExcludedProduct> ArbitrageCounterpartyExcludedProducts { get; set; }

        public ArbitrageCounterparty()
        {
            ArbitrageCounterpartyExcludedProducts = new List<ArbitrageCounterpartyExcludedProduct>();
        }

        // helper function to retrieve list of Excluded Product References, 
        // these are later saved in the puma_mde_arbitrage_cparty_ep table
        public List<String> GetExcludedProductReferences(int id)
        {
            var acep = ArbitrageCounterpartyExcludedProducts.Where(x => x.ArbitrageCounterpartyId == id);
            var epr  = acep.Select(y => y.ExcludedProductReference).ToList<String>();

            return epr;
        }

        public override String ToString()
        {
            return String.Format("ArbitrageCounterparty<" +
                     "Id: {0}, "                 +
                     "Counterparty: {1}, "       +
                     "StartDate: {2}, "          +
                     "EndDate: {3}, "            +
                     "ClassificationName: {4}, " +
                     "ClassificationId: {5}, "   +
                     "Enabled: {6}, "            +
                     "IsRowUpdated: {7}, "       +
                     "IsRowDeleted: {8}, "       +
                     "IsRowNew: {9}>",
                     Id,
                     Counterparty,
                     StartDate.ToShortDateString(),
                     EndDate.ToShortDateString(),
                     ClassificationName,
                     ClassificationId,
                     Enabled,
                     IsRowUpdated,
                     IsRowDeleted,
                     IsRowNew);
        }
    }

    // data from: select * from puma_mde_arbitrage_cparty_ep
    [Serializable]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("191D3F75-C73A-4173-82C8-593CB68BA674")]
    public class ArbitrageCounterpartyExcludedProduct
    {
        public int                   Id                       { get; set; }
        public int                   ArbitrageCounterpartyId  { get; set; }
        public string                ExcludedProductReference { get; set; }
        public ArbitrageCounterparty ArbitrageCounterparty    { get; set; }

        public override String ToString()
        {
            return String.Format("ArbitrageCounterpartyExcludedProduct<" +
                     "ArbitrageCounterpartyId: {0}, " +
                     "ExcludedProductReference: {1}>",
                     ArbitrageCounterpartyId, 
                     ExcludedProductReference);
        }
    }

    // container for ArbitrageCounterpartyExcludedProduct(s)
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("27ECE012-89F4-44ac-8FEF-A071E17B0680")]
    public class ArbitrageCounterpartyExcludedProducts : IEnumerable
    {
        IList<ArbitrageCounterpartyExcludedProduct> collection;
        public ArbitrageCounterpartyExcludedProducts(IList<ArbitrageCounterpartyExcludedProduct> collection)
        {
            this.collection = collection;
        }

        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        public void Add(ArbitrageCounterpartyExcludedProduct item)
        {
            collection.Add(item);
        }

        public void Clear()
        {
            collection.Clear();
        }

        public ArbitrageCounterpartyExcludedProduct this[int index]
        {
            get { return collection[index];  }
            set { collection[index] = value; }
        }

        public int Count
        {
            get { return collection.Count; }
        }
    }
}