using System;
using Puma.MDE.Common.Utilities;

namespace Puma.MDE.Common
{
    public abstract class Audit : Entity
    {
        [IgnoreCompare]
        public DateTime JournalTimeStamp { get; set; }

        [IgnoreCompare]
        public string JournalType { get; set; }

        [IgnoreCompare]
        public abstract string EntityName { get; }

        public virtual void Compare<T>(T instance, Action<PropertyValueChange> propertyValueChange) where T : class
        {
            var compareToInstance = instance;
            CompareUtil.Compare(this, compareToInstance, propertyValueChange);
        }

        public virtual void Compare(object instance, Action<PropertyValueChange> propertyValueChange)
        {
            var compareToInstance = instance;
            CompareUtil.Compare(this, compareToInstance, propertyValueChange);
        }

        public virtual bool IsInsertOrDelete(Action<PropertyValueChange> propertyValueChange)
        {
            return CompareUtil.IsInsertOrDelete(this, propertyValueChange);
        }

    }
}
