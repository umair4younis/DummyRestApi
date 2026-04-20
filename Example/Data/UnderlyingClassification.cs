using Puma.MDE.Common;
using Puma.MDE.Common.Utilities;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("AD89C676-7ED6-48c7-8970-86E6F1091333")]
    [ComVisible(true)]

    public class UnderlyingClassification
    {
        public int Id { get; set; }
        public int UnderlyingId { get; set; }
        public int ClassificationId { get; set; }

        Classification _cls = null;

        public Classification Classification
        {
            get
            {
                if (_cls == null || ClassificationId != _cls.Id)
                    _cls = Engine.Instance.Factory.GetClassification(ClassificationId);
                return _cls;
            }
        }

        Underlying _und= null;
        public Underlying Underlying
        {
            get
            {
                if (_und==null || UnderlyingId!=_und.Id)
                    _und = Engine.Instance.Factory.GetUnderlying(UnderlyingId);
                return _und;
            }
        }
    }

    public class UnderlyingClassificationAudit : Audit, IEquatable<UnderlyingClassificationAudit>
    {
        [IgnoreCompare]
        public int UnderlyingId { get; set; }

        [IgnoreCompare]
        public int ClassificationId { get; set; }

        [IgnoreCompare]
        public override string EntityName
        {
            get { return "Classification"; }
        }

        public override void Compare(object instance, Action<PropertyValueChange> propertyValueChange)
        {
            var specific = (UnderlyingClassification)instance;

            CompareUtil.Compare(this, specific, propertyValueChange);

        }

        public bool Equals(UnderlyingClassificationAudit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.UnderlyingId == UnderlyingId && other.ClassificationId == ClassificationId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (UnderlyingClassificationAudit)) return false;
            return Equals((UnderlyingClassificationAudit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (UnderlyingId*397) ^ ClassificationId;
            }
        }

        public static bool operator ==(UnderlyingClassificationAudit left, UnderlyingClassificationAudit right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(UnderlyingClassificationAudit left, UnderlyingClassificationAudit right)
        {
            return !Equals(left, right);
        }
    }

    public class UnderlyingClassificationData : Entity
    {
        private string _underlyingReference;
        public string UnderlyingReference
        {
            get { return _underlyingReference; }
            set { _underlyingReference = value; NotifyPropertyChanged(() => UnderlyingReference); }
        }

        private string _classification;
        public string Classification
        {
            get { return _classification; }
            set { _classification = value; NotifyPropertyChanged(() => Classification); }
        }

        private string _effective;
        public string Effective
        {
            get { return _effective; }
            set { _effective = value; NotifyPropertyChanged(() => Effective); }
        }
    }
}