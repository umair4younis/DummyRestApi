
using System;

namespace Puma.MDE.Common.Utilities
{
    /// <summary>
    /// A generic class that is used to identify changes of 
    /// properties of one instance to another instance
    /// </summary>
    public class PropertyValueChange : Entity
    {
        public PropertyValueChange() { }

        public PropertyValueChange(PropertyValueChange c)
        {
            ChangedParameter = c.ChangedParameter;
            ChangedParameterPrevValue = c.ChangedParameterPrevValue;
            ChangedParameterNewValue = c.ChangedParameterNewValue;
            ChangedParameterStatus = c.ChangedParameterStatus;
            ChangedEntity = c.ChangedEntity;
            ChangedTimeStamp = c.ChangedTimeStamp;
        }

        private string _id;

        /// <summary>
        /// An identifier, id ex(204) of database or another field perhaps 
        /// Underlying.Refrence that used as reference to a unique entity (underlying)
        /// </summary>
        public new string Id
        {
            get { return _id; }
            set { 
                 NotifyPropertyChangedDirty(ref _id, value, () => Id);
            }
        }

        private string _changedParameter;

        /// <summary>
        /// Property name whose value has changed inside and c# object instance 
        /// for ex Underlying.RIC that have changed in value
        /// </summary>
        public string ChangedParameter
        {
            get { return _changedParameter; }
            set { _changedParameter = value; NotifyPropertyChanged(() => ChangedParameter); }
        }

        private string _changedEntity;

        /// <summary>
        /// ChangedEntityname whose Property value has changed inside and c# object instance 
        /// for ex Underlying whose RIC that have changed in value
        /// </summary>
        public string ChangedEntity
        {
            get { return _changedEntity; }
            set { _changedEntity = value; NotifyPropertyChanged(() => ChangedEntity); }
        }

        private object _changedParameterPrevValue;

        /// <summary>
        /// previous value Ric = 'a value'
        /// </summary>
        public object ChangedParameterPrevValue
        {
            get { return _changedParameterPrevValue; }
            set { _changedParameterPrevValue = value; NotifyPropertyChanged(() => ChangedParameterPrevValue); }
        }

        private object _changedParameterNewValue;

        /// <summary>
        /// new value Ric = 'a different value'
        /// </summary>
        public object ChangedParameterNewValue
        {
            get { return _changedParameterNewValue; }
            set { _changedParameterNewValue = value; NotifyPropertyChanged(() => ChangedParameterNewValue); }
        }

        private string _changedParameterStatus;

        /// <summary>
        /// 'Added', 'Deleted' or 'Changed' as its value
        /// </summary>
        public string ChangedParameterStatus
        {
            get { return _changedParameterStatus; }
            set { _changedParameterStatus = value; NotifyPropertyChanged(() => ChangedParameterStatus); }
        }

        private DateTime _changedTimeStamp;

        /// <summary>
        /// Time that has either 'Added', 'Deleted' or 'Changed'
        /// </summary>
        public DateTime ChangedTimeStamp
        {
            get { return _changedTimeStamp; }
            set { _changedTimeStamp = value; NotifyPropertyChanged(() => ChangedTimeStamp); }
        }
    }
}
