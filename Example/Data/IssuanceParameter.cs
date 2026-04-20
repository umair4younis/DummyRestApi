using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using System.Reflection;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("5CA68E98-A0F6-42d5-9765-5FA0491C71F7")]
    public class IssuanceParameters : IEnumerable
    {
        IList<IssuanceParameter> collection;
        public IssuanceParameters(IList<IssuanceParameter> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(IssuanceParameter item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public IssuanceParameter this[int index]
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
        public IList<IssuanceParameter> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("AC3AB688-B9D0-4b53-9D8C-0D6496C4B836")]
    [ComVisible(true)]

    public class IssuanceParameter
    {
        public int Id { get; set; }
        public int ClassificationId { get; set; }
        int prodDescrId = 0;
        public int ProductDescriptionId {
            get
            {
                if (_productDescription != null)
                    prodDescrId = _productDescription.Id;

                return prodDescrId;
            }
            set
            {
                prodDescrId = value;
            }
        }
        public string SpreadTab { get; set; }
        public string ConstVolaMarkup { get; set; }
        public string StdVolAmtTab { get; set; }
        public string MarginStartValueTab { get; set; }
        public string MarginEndValueTab { get; set; }
        public string LongTermMaturity { get; set; }
        public string ConstantVolMarkupFactor { get; set; }
        public string ManQVol { get; set; }

        public string CloPrcDevTab { get; set; }
        public string CloPrcDevTabRes { get; set; }
        public string MarginDistribution { get; set; }
        public string MarginType { get; set; }
        public string MaxNRefillTab { get; set; }
        public string MaxPrcDevTab { get; set; }
        public string MaxPrcDevTabRes { get; set; }
        public string MinMarkup { get; set; }
        public string MinPrcDevTab { get; set; }
        public string OwnPrcAskFloor { get; set; }
        public string OwnPrcFloor { get; set; }
        public string PrcThreshold { get; set; }
        public string QVolMtd { get; set; }
        public string QRTimeout { get; set; }
        public string SoldInLower { get; set; }
        public string SpreadBias { get; set; }
        public string SpreadBiasRes { get; set; }
        public string SpreadMult { get; set; }
        public string SpreadMultRes { get; set; }
        public string SpreadTabIndex { get; set; }
        public string SpreadType { get; set; }
        public string TRTimeout { get; set; }
        public string TSpreadMultTab { get; set; }
        public string TVolMultTab { get; set; }
        public string VolaShiftEndValueTab { get; set; }
        public string VolaShiftStartValueTab { get; set; }
        public string RoundVolTab { get; set; }
        public string CloPrcDevFast { get; set; }
        public string MaxPrcDevFast { get; set; }
        public string RefillSuspTime { get; set; }
        public string SpreadMultFast { get; set; }
        public string RefillMultfast { get; set; }
        public string AskCapMethod { get; set; }
        public string PumaPricingCalculationPolicy { get; set; }
        public string SoldOutThreshold { get; set; }
        public string MarginTabIndex { get; set; }
        public string MaxTrdVol { get; set; }
        public string MaxTrdVolTime { get; set; }
        public string MaxTrdVolType { get; set; }
        public string RejPrcThreshold { get; set; }
		public string MinQuotedVolume { get; set; }
		public string MuDecFactor { get; set; }
		public string TimedMaxNRefill { get; set; }
        public string MinPriceDev { get; set; }
        public string MidPriceDev { get; set; }
        public string MinQuoteInterval { get; set; }
        public string CertSpreadMult { get; set; }
        public string SpreadBiasTabIndex { get; set; }
        public string QRMode { get; set; }
        public string MarginCrisisValueTab { get; set; }
        public string VolaCrisisValueTab { get; set; }

        public string MinPriceDevType { get; set; }
        public string DeltaThr { get; set; }
        public string MaxSophisFvDevTab { get; set; }

        public string MrgFadeOut { get; set; }
        public string OwnPrcAskFloorMultiplier { get; set; }
        public string OptMarginTabIndex { get; set; }
        public string OptMarginStartValueTab { get; set; }
        public string OptMarginEndValueTab { get; set; }
        public string OptMarginStartDate { get; set; }
        public string OptMarginEndDate { get; set; }
        public string FundingFactor { get; set; }
        public string QRModeReset { get; set; }
        public string PumaAskFVCheckOverride { get; set; }
        
        public string AddVolSpeadTab { get; set; }
        public string VegaSpreadTab { get; set; }

        public string MrgFadeOutThreshold { get; set; } // #31452
        public string BidOnlyBeforeExp { get; set; }    // #31452
        public string ThetaMarginTab { get; set; }      // #31452
        public string RealizedThreshold { get; set; }   // #31452
        public string UnrealizedThreshold { get; set; } // #31452

        public string SophisFolioID { get; set; }       // #31273

        public string MaxSpreadTab { get; set; }        // #31972
        public string MaxSpreadMethod { get; set; }     // #31972
        public string TrMaxDevAmount { get; set; }      // #31972
        public string TrMaxDevSpread { get; set; }      // #31972
        public string TrDevSpreadType { get; set; }     // #31972
        public string TrDevSpreadAmount { get; set; }   // #31972
        public string VolaShiftTabIndex { get; set; }   // #31972

        public string RFQVolThresholdMult { get; set; }      // #34501
        public string CrisisMarkupMultiplier { get; set; }   // #34501
        public string DeltaThrUpDown { get; set; }           // #34501

        public string DownShiftTimeSpanTab  { get; set; }   // #36314
        public string DownShiftValueTab     { get; set; }   // #36314

        public string ExtraSpreadMult       { get; set; }   // #37813
        public string ExtraSpreadMultIndex  { get; set;}    // #37813
        
        public string AddSpreadMkpTime  { get; set;}       // #44209
        public string IntPriceOffset  { get; set;}         // #44209
        public string IntVolMultiplier  { get; set;}       // #44209

        public string MinSpread { get; set; }               // EQID-4603
        public string MinSpreadType { get; set; }           // EQID-4603


        public string MaxPrcDevTabYieldCurve        { get; set;}       // #47299
        public string MaxPrcDevTabYieldCurveReset   { get; set;}       // #47299
        public string ClosePrcDevTabYieldCurve      { get; set;}       // #47299
        public string ClosePrcDevTabYieldCurveReset { get; set;}       // #47299

        public string SpreadBiasAdjustment          { get; set; }      // #50852
        public string NormalizeSpread               { get; set; }      // #53964

        public string ReverseThetaMarginTab         { get; set; }      // #55088
        public string CertSpreadVolType             { get; set; }      // #55088

        public string BarrierAutoConf               { get; set; }       //55248
        public string BarrierAutoConfObsPeriod      { get; set; }       //55248
        public string BarrierAutoConfPriceThreshold { get; set; }       //55248
        public string ReverseThetaMarginDTM         { get; set; }       //55701
        public string OptMkpTime                    { get; set; }       //56017

        public string AddTrMaxDevAmount             { get; set; }       //57761
        public string AddTrMaxDevSpread             { get; set; }       //57761

        public string DownShiftValueTabIndex        { get; set; }       //62835
        public string DownShiftTimeSpanTabIndex     { get; set; }       //62835
        public string LevSpreadTabMult              { get; set; }       //65220
        public string MrgMult                       { get; set; }       //65219


        ProductDescription _productDescription = null;
        public ProductDescription ProductDescription
        {
            get
            {
                if ( _productDescription != null)
                    return _productDescription;

                if (prodDescrId > 0)
                    _productDescription = Engine.Instance.Factory.GetProductDescription(prodDescrId);

                return _productDescription;
            }
            set
            {
                _productDescription = value;
                if (value != null)
                    prodDescrId = value.Id;
                else
                    prodDescrId = 0;
            }
        }


        public object GetAttributeValue(string name)
        {
            PropertyInfo prop = GetType().GetProperty(name);
            return prop.GetValue(this, null);
        }
        public void SetAttributeValue(string name, object value)
        {
            PropertyInfo prop = GetType().GetProperty(name);
            prop.SetValue(this, value, null);
        }
    }
}
