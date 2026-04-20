using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;
using System.ComponentModel;
using Puma.MDE.Common.Utilities;
using Puma.MDE.Common;


namespace Puma.MDE.Data
{
    [ComVisible(false)]
    public class ExtrapolationSlices : IEnumerable
    {
        IList<ExtrapolationSlice> collection;
        public ExtrapolationSlices(IList<ExtrapolationSlice> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(ExtrapolationSlice item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public ExtrapolationSlice this[int index]
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


    public enum ExtrapolationTypeEnum
    {
        DeltaTShift,
        DeltaTShiftAndReferenceSkewConvexity,
        DeltaTShiftAndTargetSkewConvexity
    }

    [ComVisible(false)]
    [Serializable]
    public class ExtrapolationSlice : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public String Bucket { get; set; }

        public string ExtrapolationTypeName
        {
            get
            {
                return Enum.GetName(typeof(ExtrapolationTypeEnum), ExtrapolationType);
            }
        }

        ExtrapolationTypeEnum extrapolationType;
        public ExtrapolationTypeEnum ExtrapolationType 
        {
            get
            {
                return extrapolationType;
            }
            set
            {
                extrapolationType = value;

                if (extrapolationType != ExtrapolationTypeEnum.DeltaTShiftAndReferenceSkewConvexity)
                    ReferenceReference = "";

                if (extrapolationType == ExtrapolationTypeEnum.DeltaTShift)
                {
                    Skew = null;
                    CallCnv = null;
                    PutCnv = null;
                }

                NotifyPropertyChanged("ExtrapolationType");
                NotifyPropertyChanged("IsReferenceEnabled");
                NotifyPropertyChanged("AreValuesEnabled");
            }
        }

        public SettingsSetRule Settings {get;set;}

        public int ReferenceId	{get;set;}

        public string Reference
        {
            get
            {
                return ReferenceReference;
            }
        }

        public string ReferenceReference
        {
            get
            {
                Underlying reference = null;
                try
                {
                    reference = Engine.Instance.Factory.GetUnderlying(ReferenceId);
                }
                catch
                {
                }

                if (reference == null)
                    return "";
                else
                    return reference.Reference;
            }
            set
            {
                Underlying reference = null;
                try
                {
                    reference = Engine.Instance.Factory.GetUnderlying(value);
                }
                catch
                {
                }

                if (reference == null)
                    ReferenceId = 0;
                else
                    ReferenceId = reference.Id;

                NotifyPropertyChanged("ReferenceReference");
                NotifyPropertyChanged("Reference");
                NotifyPropertyChanged("ReferenceId");
            }
        }

        double? skew;
        public double? Skew 
        {
            get
            {
                return skew;
            }
            set
            {
                skew = value;
                NotifyPropertyChanged("Skew");
            }
        }

        double? putCnv;
        public double? PutCnv
        {
            get
            {
                return putCnv;
            }
            set
            {
                putCnv = value;
                NotifyPropertyChanged("PutCnv");
            }
        }

        double? callCnv;
        public double? CallCnv
        {
            get
            {
                return callCnv;
            }
            set
            {
                callCnv = value;
                NotifyPropertyChanged("CallCnv");
            }
        }

        public DateTime Maturity
        {
            get
            {
                try
                {
                    return DateTime.ParseExact(Bucket, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    return new DateTime();
                }
            }
            set
            {
                Bucket = value.ToString("yyyy-MM-dd");
            }
        }

        public ExtrapolationSlice Clone()
        {
            return new ExtrapolationSlice() {CallCnv=this.CallCnv, PutCnv=this.PutCnv, Skew=this.Skew, Bucket=this.Bucket, ReferenceId=this.ReferenceId, ExtrapolationType=this.ExtrapolationType };
        }

        public bool IsReferenceEnabled
        {
            get
            {
                return ExtrapolationType == ExtrapolationTypeEnum.DeltaTShiftAndReferenceSkewConvexity;
            }
        }
        public bool AreValuesEnabled
        {
            get
            {
                return ExtrapolationType != ExtrapolationTypeEnum.DeltaTShift;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }

    public class FoExtrapolationSliceAudit : Audit, IEquatable<FoExtrapolationSliceAudit>
    {
        public String Bucket { get; set; }

        public string ExtrapolationTypeName
        {
            get
            {
                return Enum.GetName(typeof(ExtrapolationTypeEnum), ExtrapolationType);
            }
        }

        ExtrapolationTypeEnum extrapolationType;
        public ExtrapolationTypeEnum ExtrapolationType
        {
            get
            {
                return extrapolationType;
            }
            set
            {
                extrapolationType = value;

                if (extrapolationType != ExtrapolationTypeEnum.DeltaTShiftAndReferenceSkewConvexity)
                    ReferenceReference = "";

                if (extrapolationType == ExtrapolationTypeEnum.DeltaTShift)
                {
                    Skew = null;
                    CallCnv = null;
                    PutCnv = null;
                }
            }
        }

        //[IgnoreCompare]
        //public FoSettingsSetRule Settings { get; set; }
        [IgnoreCompare]
        public int ReferenceId { get; set; }

        [IgnoreCompare]
        public string Reference
        {
            get
            {
                return ReferenceReference;
            }
        }

        [IgnoreCompare]
        public string ReferenceReference
        {
            get
            {
                Underlying reference = null;
                try
                {
                    reference = Engine.Instance.Factory.GetUnderlying(ReferenceId);
                }
                catch
                {
                }

                if (reference == null)
                    return "";
                else
                    return reference.Reference;
            }
            set
            {
                Underlying reference = null;
                try
                {
                    reference = Engine.Instance.Factory.GetUnderlying(value);
                }
                catch
                {
                }

                if (reference == null)
                    ReferenceId = 0;
                else
                    ReferenceId = reference.Id;
            }
        }

        public double? Skew { get; set; }

        public double? PutCnv { get; set; }

        public double? CallCnv { get; set; }

        [IgnoreCompare]
        public DateTime Maturity
        {
            get
            {
                try
                {
                    return DateTime.ParseExact(Bucket, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    return new DateTime();
                }
            }
            set
            {
                Bucket = value.ToString("yyyy-MM-dd");
            }
        }

        public bool IsReferenceEnabled
        {
            get
            {
                return ExtrapolationType == ExtrapolationTypeEnum.DeltaTShiftAndReferenceSkewConvexity;
            }
        }
        
        public bool AreValuesEnabled
        {
            get
            {
                return ExtrapolationType != ExtrapolationTypeEnum.DeltaTShift;
            }
        }

        #region Overrides of Audit

        [IgnoreCompare]
        public override string EntityName
        {
            get { return "Extrapolation Slice"; }
        }

        public override void Compare(object instance, Action<PropertyValueChange> propertyValueChange)
        {
            var specific = (FoExtrapolationSliceAudit)instance;
            CompareUtil.Compare(this, specific, propertyValueChange);
        }

        #endregion

        public bool Equals(FoExtrapolationSliceAudit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Bucket, Bucket) && Equals(other.extrapolationType, extrapolationType) && other.ReferenceId == ReferenceId && other.Skew.Equals(Skew) && other.PutCnv.Equals(PutCnv) && other.CallCnv.Equals(CallCnv);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (FoExtrapolationSliceAudit)) return false;
            return Equals((FoExtrapolationSliceAudit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (Bucket != null ? Bucket.GetHashCode() : 0);
                result = (result * 397) ^ (JournalType != null ? JournalType.GetHashCode() : 0);
                result = (result * 397) ^ JournalTimeStamp.GetHashCode();
                result = (result*397) ^ extrapolationType.GetHashCode();
                result = (result*397) ^ ReferenceId;
                result = (result*397) ^ (Skew.HasValue ? Skew.Value.GetHashCode() : 0);
                result = (result*397) ^ (PutCnv.HasValue ? PutCnv.Value.GetHashCode() : 0);
                result = (result*397) ^ (CallCnv.HasValue ? CallCnv.Value.GetHashCode() : 0);
                return result;
            }
        }

        public static bool operator ==(FoExtrapolationSliceAudit left, FoExtrapolationSliceAudit right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FoExtrapolationSliceAudit left, FoExtrapolationSliceAudit right)
        {
            return !Equals(left, right);
        }
    }

    
}
