using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Runtime.InteropServices;

// Trac: #37794 - MDE: Add SmartOrc Project for Auomatic Quotation Updates
namespace Puma.MDE.Data
{
    // data from: select * from puma_mde_orc_bid_only
    public class OrcBidOnly
    {
        public int      Id                 { get; set; }
        public string   ProductType        { get; set; }
        public DateTime StartDate          { get; set; }
        public DateTime EndDate            { get; set; }
        public String   ClassificationName { get; set; }
        public int      ClassificationId   { get; set; }
        public bool     Enabled            { get; set; }

        // parameters for handling adding, removing or updating data in the grid
        public bool IsRowUpdated { get; set; }
        public bool IsRowDeleted { get; set; }
        public bool IsRowNew     { get; set; }

        // container for the OrcBidOnlyExcludedProduct(s)
        [ComVisible(false)]
        public IList<OrcBidOnlyExcludedProduct> OrcBidOnlyExcludedProducts { get; set; }

        public OrcBidOnly()
        {
            OrcBidOnlyExcludedProducts = new List<OrcBidOnlyExcludedProduct>();
        }

        // helper function to retrieve list of Excluded Product References, 
        // these are later saved in the puma_mde_orc_bid_only_ep table
        public List<String> GetExcludedProductReferences(int id)
        {
            var oboep = OrcBidOnlyExcludedProducts.Where(x => x.OrcBidOnlyId == id);
            var epr   = oboep.Select(y => y.ExcludedProductReference).ToList<String>();

            return epr;
        }

        public override String ToString()
        {
            return String.Format("OrcBidOnly<" +
                     "Id: {0}, " +
                     "ProductType: {1}, " +
                     "StartDate: {2}, " +
                     "EndDate: {3}, " +
                     "ClassificationName: {4}, " +
                     "ClassificationId: {5}, " +
                     "Enabled: {6}, " +
                     "IsRowUpdated: {7}, " +
                     "IsRowDeleted: {8}, " +
                     "IsRowNew: {9}>",
                     Id,
                     ProductType,
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

    // data from: select * from seq_puma_mde_orc_bid_only_ep
    [Serializable]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("285B188D-A054-4ded-8973-806238E3BEF9")]
    public class OrcBidOnlyExcludedProduct
    {
        public int        Id                       { get; set; }
        public int        OrcBidOnlyId             { get; set; }
        public string     ExcludedProductReference { get; set; }
        public OrcBidOnly OrcBidOnly               { get; set; }

        public override String ToString()
        {
            return String.Format("OrcBidOnlyExcludedProduct<" +
                     "OrcBidOnlyId: {0}, " +
                     "ExcludedProductReference: {1}>",
                     OrcBidOnlyId,
                     ExcludedProductReference);
        }
    }

    // container for OrcBidOnlyProduct(s)
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("6581C050-9E70-4bcf-8064-7982AF5358F2")]
    public class OrcBidOnlyExcludedProducts : IEnumerable
    {
        IList<OrcBidOnlyExcludedProduct> collection;
        public OrcBidOnlyExcludedProducts(IList<OrcBidOnlyExcludedProduct> collection)
        {
            this.collection = collection;
        }

        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        public void Add(OrcBidOnlyExcludedProduct item)
        {
            collection.Add(item);
        }

        public void Clear()
        {
            collection.Clear();
        }

        public OrcBidOnlyExcludedProduct this[int index]
        {
            get { return collection[index]; }
            set { collection[index] = value; }
        }

        public int Count
        {
            get { return collection.Count; }
        }
    }
}
