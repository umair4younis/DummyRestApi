using Puma.MDE.Common;
using Puma.MDE.Common.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("ED730E02-9E4B-4d49-B253-DBD6AF7E8C77")]
    public class ExtrapolationValues : IEnumerable
    {
        IList<ExtrapolationValue> collection;
        public ExtrapolationValues(IList<ExtrapolationValue> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(ExtrapolationValue item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public ExtrapolationValue this[int index]
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
    [Guid("F3E96457-7FE0-451f-ABD9-3C6F5F0E6A67")]
    [ComVisible(true)]

    public class ExtrapolationValue : IEquatable<ExtrapolationValue>
    {
        public int Id { get; set; }
        public String Name { get; set; }
        public double Value { get; set; }
        public String ValuAsString { get; set; }

        [IgnoreCompare]
        public Extrapolation Extrapolation { get; set; }

        #region Equality
        public bool Equals(ExtrapolationValue other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Id == Id && Equals(other.Name, Name) && other.Value.Equals(Value) && Equals(other.ValuAsString, ValuAsString);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (ExtrapolationValue)) return false;
            return Equals((ExtrapolationValue) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = Id;
                result = (result*397) ^ (Name != null ? Name.GetHashCode() : 0);
                result = (result*397) ^ Value.GetHashCode();
                result = (result*397) ^ (ValuAsString != null ? ValuAsString.GetHashCode() : 0);
                return result;
            }
        }

        public static bool operator ==(ExtrapolationValue left, ExtrapolationValue right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ExtrapolationValue left, ExtrapolationValue right)
        {
            return !Equals(left, right);
        }
        #endregion
    }

    public class FoExtrapolationValue
    {
        public int Id { get; set; }
        public String Name { get; set; }
        public double Value { get; set; }
        public String ValuAsString { get; set; }

        [IgnoreCompare]
        public FoExtrapolation Extrapolation { get; set; }
    }


    public class FoExtrapolationValueAudit : Audit, IEquatable<FoExtrapolationValueAudit>
    {
        [IgnoreCompare]
        public int ExtrapolationId { get; set; }

        public String Name { get; set; }
        public double Value { get; set; }
        public String ValuAsString { get; set; }

        [IgnoreCompare]
        public override string EntityName
        {
            get { return "Extrapolation Value"; }
        }

        public override void Compare(object instance, Action<PropertyValueChange> propertyValueChange)
        {
            var specific = (FoExtrapolationValueAudit)instance;
            CompareUtil.Compare(this, specific, propertyValueChange);
        }

        public bool Equals(FoExtrapolationValueAudit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.ExtrapolationId == ExtrapolationId && Equals(other.Name, Name) && other.Value.Equals(Value) && Equals(other.ValuAsString, ValuAsString);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (FoExtrapolationValueAudit)) return false;
            return Equals((FoExtrapolationValueAudit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = ExtrapolationId;
                result = (result*397) ^ (Name != null ? Name.GetHashCode() : 0);
                result = (result*397) ^ Value.GetHashCode();
                result = (result*397) ^ (ValuAsString != null ? ValuAsString.GetHashCode() : 0);
                result = (result * 397) ^ (JournalType != null ? JournalType.GetHashCode() : 0);
                result = (result * 397) ^ JournalTimeStamp.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(FoExtrapolationValueAudit left, FoExtrapolationValueAudit right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FoExtrapolationValueAudit left, FoExtrapolationValueAudit right)
        {
            return !Equals(left, right);
        }
    }

}
