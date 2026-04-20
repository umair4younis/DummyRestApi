using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows.Threading;
using Puma.MDE.Common.Utilities;


namespace Puma.MDE.Common
{
    [Serializable]
    public abstract class Entity : INotifyPropertyChanged, IEditableObject
    {
        private int _id;
        [IgnoreCompare]
        public int Id
        {
            get { return _id; }
            protected set { _id = value; }
        }

        /// <summary>
        /// This create int identity usually using hashcode and it is up to developer to override and decide how to generate identity.
        /// A usecase is to remember or record an identity value for a record(row) in grid and when refresh
        /// is done we can reselect the same record(row) in the grid. ofcourse the Id of the same record may change due to its value being updated. 
        /// ex brand new object that ois added to grid and is transient when refreshed its id will be set but it will have a different 
        /// identity if it was based on Id field. The record is the same but has evolved
        /// </summary>
        [ComVisible(false)]
        public virtual int? Identity() { return null; }

        #region Construction

        [IgnoreCompare]
        //private readonly Dispatcher _originalDispatcher;
        protected Entity()
        {
            // Getting the dispather at the point of creation which could be UI thread dispather or background thread one
            // If this object is created using a backgoround thread then it must be changed using this original thread
            // But notification change must happen in UI thread
            //_originalDispatcher = Dispatcher.CurrentDispatcher;
            IsNotificationEnabled = true;
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context)
        {
            if (_entityStatus == null)
                _entityStatus = new Lazy<EntityStatus>();
        }
        #endregion

        #region Object Database/Runtime Status
        /// <summary>
        /// Is a newly created unsaved object and hence no Id set, id = 0
        /// </summary>
        [IgnoreCompare]
        public bool IsTransient { get { return Id == 0; } }

        private static bool IsTransientEx(Entity obj)
        {
            return obj != null && Equals(obj.Id, 0);
        }

        /// <summary>
        /// Is an object that was once saved and hence has an Id set
        /// </summary>
        [IgnoreCompare]
        public bool IsPersisted { get { return Id != 0; } }

        /// <summary>
        /// If it is set to be deleted
        /// </summary>
        private bool _isSetToDelete;
        [IgnoreCompare]
        public bool IsSetToDelete
        {
            get { return _isSetToDelete; }
            set 
            {
                _isSetToDelete = value;

                if (_isSetToDelete)
                    SetToDirty();
            }
        }

        /// <summary>
        /// This is switch so that when we want to stop notification by INotifyPropertyChange event
        /// to stop ui be updated. Sometimes you want to avoid thread crash by updating just the properties silently
        /// you then enable after edit
        /// </summary>
        [IgnoreCompare]
        public bool IsNotificationEnabled { get; set; }

        #endregion

        #region Object Dirty Status
        private Lazy<EntityStatus> _entityStatus;
        private Lazy<EntityStatus> EntityStatus
        {
            get
            {
                if (_entityStatus == null)
                    _entityStatus = new Lazy<EntityStatus>();

                return _entityStatus;
            }
        }

        [ComVisible(false)]
        public bool IsDirty(Expression<Func<object>> expression)
        {
            return EntityStatus.Value.IsDirty(expression);
        }

        [ComVisible(false)]
        public bool AnyDirty(params Expression<Func<object>>[] expressions)
        {
            return EntityStatus.Value.AnyDirty(expressions);
        }

        [ComVisible(false)]
        public void SetToDirty(Expression<Func<object>> expression)
        {
            EntityStatus.Value.SetToDirty(expression);
        }

        public void SetToDirty(string propertyName)
        {
            EntityStatus.Value.SetToDirty(propertyName);
        }

        public void SetToDirty()
        {
            EntityStatus.Value.SetToDirty();
        }

        public bool IsDirty()
        {
            return EntityStatus.Value.IsDirty(); 
        }

        public void ResetToNonDirty()
        {
            EntityStatus.Value.ResetToNonDirty();
        }
        #endregion 

        #region IEditableObject
        [ComVisible(false)]
        [IgnoreCompare]
        private Momento<Entity> _memento;

        [ComVisible(false)]
        public void AddBeginEdit(EventHandler<EntityChangedEventArgs<Entity>> onBeginEdit)
        {
            OnBeginEdit += onBeginEdit;
        }

        [ComVisible(false)]
        public void AddCancelEdit(EventHandler<EntityChangedEventArgs<Entity>> onCancelEdit)
        {
            OnCancelEdit += onCancelEdit;
        }

        [ComVisible(false)]
        public void AddEndEdit(EventHandler<EntityChangedEventArgs<Entity>> onEndEdit)
        {
            OnEndEdit += onEndEdit;
        }

        [IgnoreCompare]
        private event EventHandler<EntityChangedEventArgs<Entity>> OnBeginEdit;
        [IgnoreCompare]
        private event EventHandler<EntityChangedEventArgs<Entity>> OnCancelEdit;
        [IgnoreCompare]
        private event EventHandler<EntityChangedEventArgs<Entity>> OnEndEdit;

        public void BeginEdit()
        {
            // Cache this object
            _memento = null;
            _memento = new Momento<Entity>(this);

            // Call the event
            var handler = OnBeginEdit;
            if (handler != null)
                handler(this, new EntityChangedEventArgs<Entity>((Entity)this, null));
        }

        public void CancelEdit()
        { 
            // Get a changed version that is going to reversed
            var recentChange = MemberwiseClone();

            // Reverse back changes
            _memento.Restore(this);
            _memento = null;

            ResetToNonDirty();

            // Call the event
            var handler = OnCancelEdit;
            if (handler != null)
                handler(this, new EntityChangedEventArgs<Entity>((Entity)recentChange, (Entity)this));
        }

        public void EndEdit()
        {
            // It must not be null
            if (_memento == null)
                throw new ArgumentNullException(string.Format("momento is null in EndEdit so BeginEdit() was not invoked"));

            // Call the event as the changes have been commited
            var handler = OnEndEdit;
            if (handler != null)
                handler(this, new EntityChangedEventArgs<Entity>((Entity)_memento.Originator, (Entity)this));

            // reset
            _memento = null;
        }

        public bool IsEditing
        {
            get { return (_memento != null);  }
        }

        #endregion

        #region INotifyPropertyChanged
        [IgnoreCompare]
        public virtual event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (!IsNotificationEnabled)
                return;

            if (PropertyChanged == null) 
                return;

            var currentDispather = Dispatcher.CurrentDispatcher;

            // If this current dispather is in UI thread by checking its access then do the property chage notification
            // Other wise marshll it to UI thread.
            if (currentDispather.CheckAccess())
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            } 
            else
            {
                currentDispather.BeginInvoke(new Action(() => PropertyChanged(this, new PropertyChangedEventArgs(propertyName))));
            }
        }

        [ComVisible(false)]
        public void NotifyPropertyChanged<T>(Expression<Func<T>> property)
        {
            var propertyName = Expressions.PropertyNameFor(property);

            // Fire property changed
            NotifyPropertyChanged(propertyName);
        }

        /// <summary>
        /// This do all inone first check if a property value changed then call the event INotify
        /// </summary>
        [ComVisible(false)]
        public void NotifyPropertyChangedDirty<T>(ref T field, T value, Expression<Func<T>> expression)
        {
            var propertyName = Expressions.PropertyNameFor(expression);

            // Check if the are equal
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                CheckAndSetDirty(propertyName, true);
                return;
            }

            // since they are not equal set the property
            field = value;

            // Notify that something has changed to subscribers
            NotifyPropertyChanged(propertyName);

            // Either inintialize (dirty = false) or (dirty = true) if itwas already initialized
            CheckAndSetDirty(propertyName, false);

        }

        [ComVisible(false)]
        public void NotifyCollectionChangedDirty<T>(ref T field, T value)
        {
            if (!(field is INotifyCollectionChanged)) 
                return;

            var type = field.GetType();
            var propertyName = type.Name;
            var handlerObject = type.GetField("CollectionChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            if (handlerObject != null)
            {
                var handler = handlerObject.GetValue(field) as Delegate;
                if (handler != null)
                {
                    if (handler.GetInvocationList().Length < 1)
                    {
                        ((INotifyCollectionChanged)field).CollectionChanged += (sender, e) => CheckAndSetDirty(propertyName, false);
                    }
                }
                else
                {
                    ((INotifyCollectionChanged)field).CollectionChanged += (sender, e) => CheckAndSetDirty(propertyName, false);
                }
            }

            // Check if the are equal
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                CheckAndSetDirty(propertyName, true);
                return;
            }

            // since they are not equal set the property
            field = value;

            // Notify that something has changed to subscribers
            NotifyPropertyChanged(propertyName);

            // Either inintialize (dirty = false) or (dirty = true) if itwas already initialized
            CheckAndSetDirty(propertyName, false);
        }

        protected void CheckAndSetDirty(string propertyName, bool areEqual)
        {
            // Lazy initializes
            var dirtyProperties = EntityStatus.Value.DirtyProperties.Value;

            var isItInDirtyList = dirtyProperties.ContainsKey(propertyName);

            if (isItInDirtyList)
            {
                if(areEqual)
                {
                    // Already initialized and may be changed, thereis no point setting to any value as we dont know
                    // if it reset dirty was called already, means it may have been changed and set again to the same value 
                    // dirtyProperties[propertyName] = false;
                }
                else
                {
                    // Already initialized and changed, so it is dirty
                    dirtyProperties[propertyName] = true;
                }
            }
            else
            {
                if (areEqual)
                {
                    // Not initialized and not in the dirty list so it is first time being constructed, add it and initialize it to not dirty
                    dirtyProperties[propertyName] = false;
                }
                else
                {
                    dirtyProperties[propertyName] = true;
                }
            }
        }

        #endregion

        #region Clone And Copy
        [ComVisible(false)]
        public T Clone<T>()
        {
            T dest;

            using (var stream = new System.IO.MemoryStream())
            {
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                formatter.Serialize(stream, this);
                stream.Position = 0;
                dest = (T)formatter.Deserialize(stream);

            }

            return dest;
            
        }

        [ComVisible(false)]
        public T ShallowCopy<T>() where T : Entity
        {
            return (T)(MemberwiseClone());
        }
        #endregion

        public virtual bool Equals(Entity other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (!IsTransientEx(this) && !IsTransientEx(other) && Equals(Id, other.Id))
            {
                var otherType = other.GetType();
                var thisType = GetType();

                return thisType.IsAssignableFrom(otherType) || otherType.IsAssignableFrom(thisType);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }

    [ComVisible(false)]
    public class EntityChangedEventArgs<D> : EventArgs where D : Entity
    {
        public D Before;
        public D After;

        public EntityChangedEventArgs(D before, D after)
        {
            Before = before;
            After = after;
        }
    }

}
