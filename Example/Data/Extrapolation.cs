using Puma.MDE.Common;
using Puma.MDE.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("53BADA27-66B3-443e-BE1F-4405E596D537")]
    [ComVisible(true)]
    [Serializable]
    public class Extrapolation
    {
        public Extrapolation()
        {
            User = Engine.Instance.ConnectedUser;
            Timestamp = DateTime.Now;
        }

        public int Id { get; set; }

        public int UnderlyingId { get; set; }

        public int UserId { get; set; }

        public int ExtrapolationModelId { get; set; }

        public DateTime Timestamp { get; set; }

        public Underlying Underlying
        {
            get
            {
                return Engine.Instance.Factory.GetUnderlying(UnderlyingId);
            }
            set
            {
                UnderlyingId = value.Id;
            }
        }

        public ExtrapolationModel ExtrapolationModel
        {
            get
            {
                return Engine.Instance.Factory.GetExtrapolationModel(ExtrapolationModelId);
            }
            set
            {
                ExtrapolationModelId = value.Id;
            }
        }

        public User User
        {
            get
            {
                return Engine.Instance.Factory.GetUser(UserId);
            }
            set
            {
                UserId = value.Id;
            }
        }

        public void Add(ExtrapolationValue p)
        {
            p.Extrapolation = this;
            Values.Add(p);
        }

        public void Add(string name, double value)
        {
            ExtrapolationValue val = Find(name);

            if (val != null)
            {
                val.Value = value;
                val.ValuAsString = "";
            }
            else
            {
                val = new ExtrapolationValue();
                val.Extrapolation = this;
                val.ValuAsString = "";
                val.Value = value;
                val.Name = name;
                Values.Add(val);
            }
        }

        public void Add(string name, string value)
        {
            ExtrapolationValue val = Find(name);

            if (val != null)
            {
                val.Value = 0;
                val.ValuAsString = value;
            }
            else
            {
                val = new ExtrapolationValue();
                val.Extrapolation = this;
                val.Name = name;
                val.Value = 0;
                val.ValuAsString = value;
                Values.Add(val);
            }
        }

        [ComVisible(false)]
        public IList<ExtrapolationValue> Values { get; set; }

        public ExtrapolationValues ValuesCollection
        {
            get
            {
                return new ExtrapolationValues(Values);
            }
        }

        ExtrapolationValue Find(string name)
        {
            foreach (ExtrapolationValue v in Values)
            {
                if (v.Name == name)
                    return v;
            }

            return null;
        }
    }

    public class FoExtrapolation
    {
        public FoExtrapolation()
        {
            User = Engine.Instance.ConnectedUser;
            Timestamp = DateTime.Now;
        }

        public int Id { get; set; }

        public int UnderlyingId { get; set; }

        [IgnoreCompare]
        public int UserId { get; set; }

        [IgnoreCompare]
        public int ExtrapolationModelId { get; set; }

        [IgnoreCompare]
        public DateTime Timestamp { get; set; }

        [IgnoreCompare]
        public Underlying Underlying
        {
            get
            {
                return Engine.Instance.Factory.GetUnderlying(UnderlyingId);
            }
            set
            {
                UnderlyingId = value.Id;
            }
        }

        [IgnoreCompare]
        public ExtrapolationModel ExtrapolationModel
        {
            get
            {
                return Engine.Instance.Factory.GetExtrapolationModel(ExtrapolationModelId);
            }
            set
            {
                ExtrapolationModelId = value.Id;
            }
        }

        [IgnoreCompare]
        public User User
        {
            get
            {
                return Engine.Instance.Factory.GetUser(UserId);
            }
            set
            {
                UserId = value.Id;
            }
        }

        public void Add(FoExtrapolationValue p)
        {
            p.Extrapolation = this;
            Values.Add(p);
        }

        public void Add(string name, double value)
        {
            FoExtrapolationValue val = Find(name);

            if (val != null)
            {
                val.Value = value;
                val.ValuAsString = "";
            }
            else
            {
                val = new FoExtrapolationValue();
                val.Extrapolation = this;
                val.ValuAsString = "";
                val.Value = value;
                val.Name = name;
                Values.Add(val);
            }
        }

        public void Add(string name, string value)
        {
            FoExtrapolationValue val = Find(name);

            if (val != null)
            {
                val.Value = 0;
                val.ValuAsString = value;
            }
            else
            {
                val = new FoExtrapolationValue();
                val.Extrapolation = this;
                val.Name = name;
                val.Value = 0;
                val.ValuAsString = value;
                Values.Add(val);
            }
        }

        [ComVisible(false)]
        public IList<FoExtrapolationValue> Values { get; set; }

      
        FoExtrapolationValue Find(string name)
        {
            foreach (FoExtrapolationValue v in Values)
            {
                if (v.Name == name)
                    return v;
            }

            return null;
        }
    }

    public class FoExtrapolationAudit : Audit, IEquatable<FoExtrapolationAudit>
    {
        [IgnoreCompare]
        public int UnderlyingId { get; set; }

        [IgnoreCompare]
        public int UserId { get; set; }

        [IgnoreCompare]
        public int ExtrapolationModelId { get; set; }

        [IgnoreCompare]
        public DateTime Timestamp { get; set; }

        [IgnoreCompare]
        public override string EntityName
        {
            get { return "Extrapolation"; }
        }

        public override void Compare(object instance, Action<PropertyValueChange> propertyValueChange)
        {
            var specific = (FoExtrapolationAudit)instance;
            CompareUtil.Compare(this, specific, propertyValueChange);
         
        }

        public bool Equals(FoExtrapolationAudit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.UnderlyingId == UnderlyingId && other.ExtrapolationModelId == ExtrapolationModelId && other.Timestamp.Equals(Timestamp);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (FoExtrapolationAudit)) return false;
            return Equals((FoExtrapolationAudit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = UnderlyingId;
                result = (result*397) ^ Id;
                result = (result * 397) ^ (JournalType != null ? JournalType.GetHashCode() : 0);
                result = (result * 397) ^ JournalTimeStamp.GetHashCode();
                result = (result*397) ^ ExtrapolationModelId;
                result = (result*397) ^ Timestamp.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(FoExtrapolationAudit left, FoExtrapolationAudit right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FoExtrapolationAudit left, FoExtrapolationAudit right)
        {
            return !Equals(left, right);
        }
    }

}
