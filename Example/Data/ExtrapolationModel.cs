using Puma.MDE.Common;
using Puma.MDE.Common.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("06E54CDC-0B85-4b10-8FB5-6FA0F684CFBC")]
    public class ExtrapolationModels : IEnumerable
    {
        IList<ExtrapolationModel> collection;
        public ExtrapolationModels(IList<ExtrapolationModel> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(ExtrapolationModel item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public ExtrapolationModel this[int index]
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
    [Guid("EB318345-037B-4816-80C9-4F93AD6627E4")]
    [ComVisible(true)]
    [Serializable]
    public class ExtrapolationModel
    {
        public ExtrapolationModel()
        {
            Parameters = new List<ExtrapolationModelParameter>();
        }
        public void Add(ExtrapolationModelParameter p)
        {
            p.Model = this;
            Parameters.Add(p);
        }

        public int Id { get; set; }
        public String Name { get; set; }
        public String FormulaReference { get; set; }
        [ComVisible(false)]
        public IList<ExtrapolationModelParameter> Parameters { get; set; }

        public ExtrapolationModelParameters ParametersCollection 
        {
            get
            {
                return new ExtrapolationModelParameters(Parameters);
            }
        }
    }

    public class FoExtrapolationModel
    {
        public FoExtrapolationModel()
        {
            Parameters = new List<FoExtrapolationModelParameter>();
        }
        public void Add(FoExtrapolationModelParameter p)
        {
            p.Model = this;
            Parameters.Add(p);
        }

        public int Id { get; set; }
        public String Name { get; set; }
        public String FormulaReference { get; set; }
        [ComVisible(false)]
        public IList<FoExtrapolationModelParameter> Parameters { get; set; }
    }

    public class FoExtrapolationModelAudit : Audit, IEquatable<FoExtrapolationModelAudit>
    {
        public String Name { get; set; }
        public String FormulaReference { get; set; }

        [IgnoreCompare]
        public override string EntityName
        {
            get { return "Extrapolation Model"; }
        }

        public override void Compare(object instance, Action<PropertyValueChange> propertyValueChange)
        {
            var specific = (FoExtrapolationModelAudit)instance;
            CompareUtil.Compare(this, specific, propertyValueChange);
        }


        public bool Equals(FoExtrapolationModelAudit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Name, Name) && Equals(other.FormulaReference, FormulaReference);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (FoExtrapolationModelAudit)) return false;
            return Equals((FoExtrapolationModelAudit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (FormulaReference != null ? FormulaReference.GetHashCode() : 0);
                result = (result * 397) ^ (JournalType != null ? JournalType.GetHashCode() : 0);
                result = (result * 397) ^ JournalTimeStamp.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(FoExtrapolationModelAudit left, FoExtrapolationModelAudit right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FoExtrapolationModelAudit left, FoExtrapolationModelAudit right)
        {
            return !Equals(left, right);
        }
    }
}
