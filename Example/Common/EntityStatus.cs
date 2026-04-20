using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Puma.MDE.Common.Utilities;


namespace Puma.MDE.Common
{
    [Serializable]
    public class EntityStatus
    {
        private Lazy<Dictionary<string, bool>> _dirtyProperties;
        public Lazy<Dictionary<string, bool>> DirtyProperties
        {
            get
            {
                if (_dirtyProperties == null)
                    _dirtyProperties = new Lazy<Dictionary<string, bool>>();

                return _dirtyProperties;
            }
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context)
        {
            if (_dirtyProperties == null)
                _dirtyProperties = new Lazy<Dictionary<string, bool>>();
        }  


        /// <summary>
        /// Check if One and only One property expression is dirty and have been changed
        /// </summary>
        public bool IsDirty(Expression<Func<object>> expression)
        {
            var propertyName = Expressions.PropertyNameFor(expression);

            // See if the the property value is dirty and has been changed after initialisation
            return (
                        DirtyProperties.Value.ContainsKey(propertyName) &&
                        DirtyProperties.Value[propertyName]
                   );
        }

        /// <summary>
        /// Check at least one property within list of property expressions is dirty and have been changed
        /// </summary>
        public bool AnyDirty(params Expression<Func<object>>[] expressions)
        {
            var isFirstDirtyItem = false;
            foreach (var expressionItems in expressions)
            {
                isFirstDirtyItem = IsDirty(expressionItems);

                if (isFirstDirtyItem)
                    break;
            }

            return isFirstDirtyItem;
        }

        /// <summary>
        /// On purposely set a property to dirty for ex wwhen an item added to a collection proiperty or removed
        /// we can set it to dirty. But this has to be hand crafted when adding or removing as List or eimple collections
        /// dont have events to observe on 
        /// </summary>
        /// <param name="expression"></param>
        public void SetToDirty(Expression<Func<object>> expression)
        {
            var propertyName = Expressions.PropertyNameFor(expression);

            DirtyProperties.Value[propertyName] = true;
        }

        public void SetToDirty(string propertyName)
        {
            DirtyProperties.Value[propertyName] = true;
        }

        public void SetToDirty()
        {
            var keys = new string[DirtyProperties.Value.Keys.Count];
            DirtyProperties.Value.Keys.CopyTo(keys, 0);
            foreach (var key in keys)
            {
                DirtyProperties.Value[key] = true;
            }
        }

        /// <summary>
        /// Check if the domain object as a whole is dirty
        /// </summary>
        public bool IsDirty()
        {
            var keys = new string[DirtyProperties.Value.Keys.Count];
            DirtyProperties.Value.Keys.CopyTo(keys, 0);
            return keys.Any(key => DirtyProperties.Value[key]);

            //var propertyInfos = this.GetPropertyInfosOnlyThisInstance();

            //var isAllDirty = false;
            //foreach (var prop in propertyInfos)
            //{
            //    isAllDirty = (
            //                    DirtyProperties.Value.ContainsKey(prop.Name) &&
            //                    DirtyProperties.Value[prop.Name]
            //                 );

            //    if (isAllDirty)
            //        break;
            //}

            //return isAllDirty;
        }

        /// <summary>
        /// This is ususaly called when saved values so that we reset the values to not dirty
        /// </summary>
        public void ResetToNonDirty()
        {
            var keys = new string[DirtyProperties.Value.Keys.Count];
            DirtyProperties.Value.Keys.CopyTo(keys, 0);
            foreach (var key in keys)
            {
                DirtyProperties.Value[key] = false;
            }
        }

    }
}
