using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;


namespace Puma.MDE.Common.Utilities
{
    [ComVisible(false)]
    [Serializable]
    public class Momento<T> where T : class
    {
        private readonly Dictionary<PropertyInfo, object> _storedProperties = new Dictionary<PropertyInfo, object>();

        public Momento(T originator)
        {
            InitializeMemento(originator);
        }

        public T Originator { get; protected set; }
        public void Restore(T originator)
        {
            foreach (var pair in _storedProperties)
            {
                pair.Key.SetValue(originator, pair.Value, null);
            }
        }

        private void InitializeMemento(T originator)
        {
            if (originator == null)
                throw new ArgumentNullException("originator", "Originator cannot be null");

            Originator = originator;

            var propertyInfos = originator.GetType()
                                          .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                                          .Where(p => p.CanRead && p.CanWrite);

            foreach (var property in propertyInfos)
                _storedProperties[property] = property.GetValue(originator, null);
        }
    }
}
