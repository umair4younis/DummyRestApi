using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Puma.MDE.Data
{

    public enum eTimePillars
    {
        [Description("None")]
        Default = 0,
        
        [Description("Curve Futures")]
        CurveFuture = 1,

        [Description("End Of Month structure")]
        EndOfMonth =2,

        [Description("Rolling Expiry Pillars")]
        RollingExpiry=3
    };


    [Guid("88B7B176-1918-48f0-A044-375237364CBB")]
    public class CommodityVolComponent
    {
        public int Id                   { get; set; }
        public string Name              { get; set; }
        public string Template          { get; set; }



        public override bool Equals(object other)
        {
            CommodityVolComponent otherComponent = other as CommodityVolComponent;
            if (otherComponent == null) return false;

            return Id == otherComponent.Id && Name == otherComponent.Name;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result =  Id;
                result = (result * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                result = (result * 397) ^ (Template != null ? Template.GetHashCode() : 0);
                return result;
            }
        }

        //public CommodityVolComponent Clone()
        //{
        //    CommodityVolComponent retval = new CommodityVolComponent();

        //    retval.              = GridWingMin;
        //    retval.GridWingMax              = GridWingMax;
        //    retval.GridWingStep             = GridWingStep;
        //    retval.GridATMMin               = GridATMMin;
        //    retval.GridATMMax               = GridATMMax;
        //    retval.GridATMStep              = GridATMStep;

        //    retval.UnderParent              = UnderParent;
        //    retval.Underlying               = Underlying ;
           
        //    return retval;
        //}

        public override string ToString()
        {
            return Name;
        }

    }


    [Guid("3E945644-9FB8-44a7-91FC-C3C05F2B9958")]
    public class CommodityVolDestination
    {
        public int Id                   { get; set; }

        public int ComponentId          { get; set; }
        public int ClassificationId     { get; set; }


        public CommodityVolDestination()
        { }
        
        public CommodityVolDestination(int componentId, int classificationId)
        {
            this.ComponentId = componentId;
            this.ClassificationId = classificationId;
        }

        public override bool Equals(object other)
        {
            CommodityVolDestination otherDestination = other as CommodityVolDestination;
            if (otherDestination == null) return false;

            return Id == otherDestination.Id && 
                         ComponentId == otherDestination.ComponentId && 
                         ClassificationId == otherDestination.ClassificationId;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = Id;
                result = (result * 397) ^ ComponentId;
                result = (result * 397) ^ ClassificationId;
                return result;
            }
        }

    }


    [Guid("D9D65959-264F-4192-85D2-5793C502BCBD")]
    public class CommodityVolParameter
    {
        public int Id { get; set; }

        public int ClassificationId     { get; set; }

        public double GridWingMin      { get; set; }
        public double GridWingMax      { get; set; }

        public double _GridWingStep = 0.04;
        public double GridWingStep
        {
            get { return _GridWingStep; }
            set
            {
                _GridWingStep = value;
                _GridWingStep = Math.Max(_GridWingStep, 0.001); // MINUMUM VALUE TO AVOID OVERFLOW
            }
        }

        public double GridATMMin       { get; set; }
        public double GridATMMax       { get; set; }
        public double _GridATMStep = 0.02;
        public double GridATMStep
        {
            get { return _GridATMStep; }
            set
            {
                _GridATMStep = value;
                _GridATMStep = Math.Max(_GridATMStep, 0.001); // MINUMUM VALUE TO AVOID OVERFLOW
            }
        }
        public bool StrikesGridAsFather { get; set; }


        public double CallShift        { get; set; }
        public double PutShift         { get; set; }
        public double UnitConversionfromFather { get; set; }
        public double VolByPriceAdjFactor { get; set; }

        public eTimePillars MaturityType{ get; set; }
        public string MaturitiesListStr { get; set; }
        public string StrikesByDeltaStr { get; set; }


        public ObservableCollection<string> GetMaturitiesCollection()
        {
            return MaturitiesListStr == null ? new ObservableCollection<string>() : null;
        }

        public List<double> GetStrikesByDelta()
        {
            return string.IsNullOrEmpty(StrikesByDeltaStr) ? new List<double>() : StrikesByDeltaStr.Split(',').Select(x => Convert.ToDouble(x)).ToList();
        }

        public void SetMaturitiesList(ObservableCollection<StringWrapper> maturitiesCollection)
        {
            if (maturitiesCollection == null) { MaturitiesListStr = null; }
            StringBuilder tempStr = new StringBuilder();
            foreach (var entry in maturitiesCollection)
            {
                tempStr.Append(entry.MaturityStr + ",");
            }
            if (tempStr.Length > 2) { tempStr.Length--; }
            MaturitiesListStr = tempStr.ToString();
        }


        //public void SetMaturitiesList(ListWrapper<string> maturitiesCollection)
        //{
        //    if (maturitiesCollection == null) { MaturitiesListStr = null; }
        //    StringBuilder tempStr = new StringBuilder();
        //    foreach (string maturity in maturitiesCollection)
        //    {
        //        tempStr.Append(maturity + ",");
        //    }
        //    if (tempStr.Length > 2) { tempStr.Length--; }
        //    MaturitiesListStr = tempStr.ToString();
        //}

        //public CommodityVolProperty()
        //{ }

        //public CommodityVolDestination(int componentId, int classificationId)
        //{
        //    this.ComponentId = componentId;
        //    this.ClassificationId = classificationId;
        //}

        //public override bool Equals(object other)
        //{
        //    CommodityVolDestination otherDestination = other as CommodityVolDestination;
        //    if (otherDestination == null) return false;

        //    return Id == otherDestination.Id &&
        //                 ComponentId == otherDestination.ComponentId &&
        //                 ClassificationId == otherDestination.ClassificationId;
        //}

        //public override int GetHashCode()
        //{
        //    unchecked
        //    {
        //        int result = Id;
        //        result = (result * 397) ^ ComponentId;
        //        result = (result * 397) ^ ClassificationId;
        //        return result;
        //    }
        //}

    }



    public class StringWrapper : Puma.MDE.Common.Entity
    {
        private string _MaturityStr;
        public string MaturityStr
        {
            get
            {
                return _MaturityStr;
            }
            set
            {
                _MaturityStr = value;
                NotifyPropertyChanged(() => MaturityStr);
            }
        }

        public StringWrapper(string lala)
        {
            this.MaturityStr = lala;
        }

        public StringWrapper()
        {
            this.MaturityStr = string.Empty;
        }

        public override string ToString()
        {
            return MaturityStr;
        }

        public bool Equals(StringWrapper other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.MaturityStr == MaturityStr;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(StringWrapper)) return false;
            return Equals((StringWrapper)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return MaturityStr.GetHashCode();
            }
        }
    }


}
