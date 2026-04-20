using Puma.MDE.Common;
using System;
using System.Collections.Generic;

namespace Puma.MDE.Data
{
    public class VegaBucket 
    {
        public int      Id              { get; set; }
        public string   Bucket          { get; set; }
        public double   Exposure        { get; set; }
        public DateTime EvaluationDate  { get; set; }
        public int      Sophis_ID       { get; set; }
        public string   NostroAccount   { get; set; }
        public int      Folio_ID        { get; set; }

        public VegaBucket() { }

        //The next 2 (very simple) constractors used only for calculations in .\mainline\SophisExtensions\PuMa.MDE\classes\PumaMDE\VolsurfaceProcessing\Processor.cs 
        public VegaBucket(int sicovam, string bucket, double exposure) 
        {
            Init(DateTime.MinValue, "", sicovam, bucket, exposure);
        }
        public VegaBucket(int sicovam, string nostroAccount, string bucket, double exposure)
        {
            Init(DateTime.MinValue, nostroAccount, sicovam, bucket, exposure);
        }


        public VegaBucket(DateTime date, int folioId, int sicovam, int bucketNum, string bucketType, double exposure)
        {
            var bucket = bucketNum.ToString() + bucketType.ToLower()[0];
            var nostroAccount = NostroCache.Instance.GetNostroForFolioId(folioId);
            Folio_ID = folioId;
            Init(date,nostroAccount, sicovam, bucket, exposure);
        }

        public VegaBucket(DateTime date, string nostroAccount, int sicovam, string bucket, double exposure)
        {
            Init(date, nostroAccount, sicovam, bucket, exposure);
        }

        private void Init(DateTime _date,string _nostroAccount, int _sicovam, string _bucket, double _exposure)
        {
            Exposure        = _exposure;
            NostroAccount   = _nostroAccount;
            Sophis_ID       = _sicovam;
            Bucket          = _bucket;
            EvaluationDate  = _date;
        }

        public VegaBucket Clone()
        {
            VegaBucket retval = new VegaBucket();

            retval.Bucket           = Bucket;
            retval.Exposure         = Exposure;
            retval.EvaluationDate   = EvaluationDate;
            retval.Sophis_ID        = Sophis_ID;
            retval.NostroAccount    = NostroAccount;
            
            return retval;
        }

        public override string ToString()
        {
            return "VegaBucket<Sicovam:" + Sophis_ID + ",Nostro:" + NostroAccount + ","+ Bucket + ",Exposure:" + string.Format("{0:###,###,###}>", Exposure);
        }

        internal class NostroCache
        {
            private Dictionary<int/*FolioID*/, string/*NostroAccount*/> nostroCache = new Dictionary<int, string>();

            static readonly NostroCache instance = new NostroCache();
            public static NostroCache Instance
            {
                get { return instance; }
            }

            public string GetNostroForFolioId(int folioID)
            {
                string nostroAccount = string.Empty;
                if (nostroCache.TryGetValue(folioID, out nostroAccount) == true)
                {
                    return nostroAccount;
                }

                nostroAccount = null;
                nostroCache.Add(folioID, nostroAccount);

                return nostroAccount;
            }
        }
    }

    public class FoRcoVega
    {
        public VegaBucket       Bucket                  { get; set; }
        public DateTime         NearestMaturitySlice    { get; set; }
        public VegaImpactLimit  VegaImpactLimit         { get; set; }

        public FoRcoVega(VegaBucket bucket)
        {
            Bucket                  = bucket;
            NearestMaturitySlice    = DateTime.MinValue;
            VegaImpactLimit         = null;
        }

        public override string ToString()
        {
            return "NearestMaturity:" + NearestMaturitySlice.ToShortDateString();
        }
    }

    public class VegaImpactLimit : Entity
    {
        private double _allowedImpact;
        public double AllowedImpact
        {
            get { return _allowedImpact; }
            set { _allowedImpact = value; NotifyPropertyChanged(() => AllowedImpact); }
        }

        private double _vegaExposure;
        public double VegaExposure
        {
            get { return _vegaExposure; }
            set { _vegaExposure = value; NotifyPropertyChanged(() => VegaExposure); }
        }

        public double AllowedImpactInPercentage
        {
            get { return  _allowedImpact/ _vegaExposure ; }
            set { _allowedImpact = value* _vegaExposure; NotifyPropertyChanged(() => AllowedImpact); NotifyPropertyChanged(() => AllowedImpactInPercentage); }
        }



        public VegaImpactLimit() { }

        public VegaImpactLimit Clone()
        {
            VegaImpactLimit retval = new VegaImpactLimit();

            retval.AllowedImpact    = AllowedImpact;
            retval.VegaExposure     = VegaExposure;
            return retval;
        }


        public bool Equals(VegaImpactLimit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.AllowedImpact == AllowedImpact && other.VegaExposure == VegaExposure;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(VegaImpactLimit)) return false;
            return Equals((VegaImpactLimit)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (AllowedImpact.GetHashCode() * 397) ^ VegaExposure.GetHashCode();
            }
        }


        public override string ToString()
        {
            return String.Format("ImpactLimit<BucketExp:{0:###,###,###},AllowedImpact:{1:###,###,###}({2:#0.##%})>", VegaExposure, AllowedImpact, AllowedImpactInPercentage);
        }
    }


}
