using Puma.MDE.Common;
using Puma.MDE.Common.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("4EE8DCE3-B29F-4623-A278-A3F2BF4EB1AF")]
    public class ExtrapolationModelParameters : IEnumerable
    {
        IList<ExtrapolationModelParameter> collection;
        public ExtrapolationModelParameters(IList<ExtrapolationModelParameter> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(ExtrapolationModelParameter item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public ExtrapolationModelParameter this[int index]
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
    [Guid("509BDB92-883F-4285-B959-228ADA291C1D")]
    [ComVisible(true)]

    public class ExtrapolationModelParameter
    {
        public ExtrapolationModelParameter()
        {
        }

        public ExtrapolationModelParameter(String name, ExtrapolationModel model)
        {
            Name = name;
            Model = model ;
        }
        public ExtrapolationModelParameter(String name)
        {
            Name = name;
        }

        public int Id { get; set; }
        public String Name { get; set; }
        public ExtrapolationModel Model {get; set;}
    }

    public class FoExtrapolationModelParameter
    {
        public FoExtrapolationModelParameter()
        {
        }

        public FoExtrapolationModelParameter(String name, FoExtrapolationModel model)
        {
            Name = name;
            Model = model ;
        }
        public FoExtrapolationModelParameter(String name)
        {
            Name = name;
        }

        public int Id { get; set; }
        public String Name { get; set; }
        public FoExtrapolationModel Model {get; set;}
    }


    public class FoExtrapolationModelParameterAudit : Audit, IEquatable<FoExtrapolationModelParameterAudit>
    {
        [IgnoreCompare]
        public int ExtrapolationModelId { get; set; }

        public String Name { get; set; }

        [IgnoreCompare]
        public override string EntityName
        {
            get { return "Extrapolation Model Parameter"; }
        }

        public override void Compare(object instance, Action<PropertyValueChange> propertyValueChange)
        {
            var specific = (FoExtrapolationModelParameterAudit)instance;
            CompareUtil.Compare(this, specific, propertyValueChange);
        }


        public bool Equals(FoExtrapolationModelParameterAudit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.ExtrapolationModelId == ExtrapolationModelId && Equals(other.Name, Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (FoExtrapolationModelParameterAudit)) return false;
            return Equals((FoExtrapolationModelParameterAudit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (ExtrapolationModelId * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                result = (result * 397) ^ (JournalType != null ? JournalType.GetHashCode() : 0);
                result = (result * 397) ^ JournalTimeStamp.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(FoExtrapolationModelParameterAudit left, FoExtrapolationModelParameterAudit right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FoExtrapolationModelParameterAudit left, FoExtrapolationModelParameterAudit right)
        {
            return !Equals(left, right);
        }
    }
}
