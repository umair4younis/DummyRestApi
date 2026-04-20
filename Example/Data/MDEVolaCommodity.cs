using System;

// Trac: #38087 - CALIBOPT/commodityfoWrapper extension for MDE vol commodity
namespace Puma.MDE.Data
{
    public class MDEVolaCommodity
    {
    }

    // details for the futures for specific commodity
    public class CommodityCurves
    {
        public int      Sicovam      { get; set; }
        public string   Reference    { get; set; }
        public string   InfoRic      { get; set; }
        public DateTime Expiry       { get; set; }
        public int      ExpiryNumber { get; set; }
        public bool     LiveCurve    { get; set; }

        public override string ToString()
        {
            return String.Format("CommodityCurves<"    +
                                 "Sicovam: {0}, "      +
                                 "Reference: {1}, "    +
                                 "InfoRic: {2}, "      + 
                                 "Expiry: {3}, "       + 
                                 "ExpiryNumber: {4}, " + 
                                 "LiveCurve: {5}>",
                                 Sicovam,
                                 Reference,
                                 InfoRic,
                                 Expiry.ToString("yyyy.MM.dd"),
                                 ExpiryNumber,
                                 LiveCurve);
        }
    }

    // currency pair
    public class CommodityCurrency
    {
        public int    CodeNum   { get; set; }    // e.g. 55923524
        public string CodeAlpha { get; set; }    //      "USD"

        public override string ToString()
        {
            return String.Format("CommodityCurrency<CodeNum: {0}, CodeAlpha: {1}>", CodeNum, CodeAlpha);
        }
    }

    // hash futures
    public class HashFutures
    {                                               // for example:
        public int      Sicovam      { get; set; }  //      89546307
        public string   Reference1   { get; set; }  //      COZ2#
        public string   Reference2   { get; set; }  //      COZ2
        public string   SecurityName { get; set; }  //      COZ2#
        public DateTime Expiry       { get; set; }  //      2022.11.15
        public DateTime DeliveryDate { get; set; }  //      2022.12.01

        public override string ToString()
        {
            return String.Format("HashFutures<"        +
                                 "Sicovam: {0}, "      +
                                 "Reference1: {1}, "   +
                                 "Reference2: {2}, "   +
                                 "SecurityName: {3}, " +
                                 "Expiry: {4}, "       +
                                 "DeliveryDate: {5}>",
                                 Sicovam,
                                 Reference1,
                                 Reference2,
                                 SecurityName,
                                 Expiry.ToString("yyyy.MM.dd"),
                                 DeliveryDate.ToString("yyyy.MM.dd"));
        }
    }

    // helper class to create FO_CommodityCurve which uses following constructor:
    //   FO_CommodityCurve::FO_CommodityCurve(
    //      long        commodity_type,
    //      long        today_date,
    //      long        comm_sicovam,
    //      long        comm_ccy_code,
    //      const char* comm_name,
    //      const char* comm_ccy,
    //      long        numberOfFutures,
    //      long*       futuresExpiries,
    //      long*       futSicovams,
    //      double*     futurePrices)
    unsafe public class CommodityCurveContext
    {
        public int     CommodityType    { get; set; }    /* long        commodity_type  */
        public int     TodayDate        { get; set; }    /* long        today_date      */
        public int     CommoditySicovam { get; set; }    /* long        comm_sicovam    */
        public int     CommodityCcyCode { get; set; }    /* long        comm_ccy_code   */
        public sbyte*  CommodityName    { get; set; }    /* const char* comm_name       */
        public sbyte*  CommodityCcy     { get; set; }    /* const char* comm_ccy        */
        public int     NumberOfFutures  { get; set; }    /* long        numberOfFutures */
        public int*    FutureExpiries   { get; set; }    /* long*       futuresExpiries */
        //public int*    FutureSicovams   { get; set; }    /* long*       futSicovams     */    // NOT NEEDED AT THE MOMENT
        public double* FuturePrices     { get; set; }    /* double*     futurePrices    */
        public int*    CommodHolidays   { get; set; }    /* long*       futSicovams     */
        public int     NumberOfHolidays { get; set; }    /* long        numberOfHolidays */
    }
}