using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Puma.MDE.Common.Utilities
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class IgnoreCompareAttribute : Attribute
    {
    }

    /// <summary>
    /// Comparing 2 instances, audited T-1 versus today T. It only add it to a list of changed items if the object 
    /// is top level and ignore deep object properties like collection of classification in Underlying but add Underlying as it is top object.
    /// Since this might be used for other instances we are passing in interestedProperties to get the 
    /// Dictionary(string, object) interestedProperties and their values in Action delegate that is called from client
    /// </summary>
    public static class CompareUtil
    {
        static class Cache
        {
            private static readonly Dictionary<string, PropertyInfo[]> PropertyInfoCache = new Dictionary<string, PropertyInfo[]>();
            private static readonly object CacheLock = new object();

            public static PropertyInfo[] CheckCache(Type instanceType)
            {

                var instanceTypeString = instanceType.FullName;
                if (instanceTypeString != null && PropertyInfoCache.ContainsKey(instanceTypeString))
                    return PropertyInfoCache[instanceTypeString];

                var proprtyInfos = instanceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                lock (CacheLock)
                {
                    if (instanceTypeString != null)
                        PropertyInfoCache[instanceTypeString] = proprtyInfos;

                    return proprtyInfos;
                }
            }

            private static void ClearCache()
            {
                lock (CacheLock)
                {
                    PropertyInfoCache.Clear();
                }
            }
        }

        /// <summary>Compare the public instance properties. Uses deep comparison.</summary>
        /// <param name="auditInstance">Yesterday recorded object that its property might have changed</param>
        /// <param name="todayInstance">Today instance of the same object whose property might have changed </param>
        /// <param name="topLevelOnly">Traverse level ( true refer to top level class and not its internal properties which also being comapred)</param>
        /// <param name="interestedPropertyList"> </param>
        /// <param name="addToList">delegate to add to client list and at the same time passing other properties that we may be interested</param>
        /// <typeparam name="TA">Audit Type of objects</typeparam>
        /// <typeparam name="TC">Latest Instance Type of object Mirror  of Audit</typeparam>
        /// <returns><see cref="bool">True</see> if both objects are equal, else <see cref="bool">false</see>.</returns>
        public static bool DeepCompare<TA, TC>(TA auditInstance,
                                          TC todayInstance,
                                          bool topLevelOnly,
                                          List<string> interestedPropertyList,
                                          Action<Dictionary<string, object>, PropertyValueChange> addToList)
            where TA : class
            where TC : class
        {
            //if (auditInstance == null || todayInstance == null || addToList == null)
            //    return auditInstance == todayInstance;

            var typeAudit = auditInstance.GetType();
            var typeLatest = todayInstance.GetType();

            // Get all propertyInfos for public properties if exist in the cache
            var auditPropertyInfos = Cache.CheckCache(typeAudit);

            var latestPropertyInfos = Cache.CheckCache(typeLatest).ToList();

            var propertyValueCollection = ExtractValues<TA>(auditInstance, interestedPropertyList, auditPropertyInfos.ToList());

            // Filter more and Get properties that dont have IgnoreCompareAttribute attribute as we are interested in them for comparing
            var propertiesInfos = auditPropertyInfos.Where(p => !Attribute.IsDefined(p, typeof(IgnoreCompareAttribute)));


            foreach (var auditPropertyInfo in propertiesInfos)
            {
                var oldValue = auditPropertyInfo.GetValue(auditInstance, null);

                // Get the corresponding latest propertyInfo
                var newValuePropertyInfo = latestPropertyInfos.SingleOrDefault(pi => pi.Name == auditPropertyInfo.Name);
                if (newValuePropertyInfo == null)
                    continue;

                // And hence the new value
                var newValue = newValuePropertyInfo.GetValue(todayInstance, null);

                // if this is a collection then it has tobe handles differently
                if (auditPropertyInfo.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(auditPropertyInfo.PropertyType))
                {
                    var collectionEqual = CheckForCollectionEquality((IEnumerable)oldValue, (IEnumerable)newValue);
                    if (collectionEqual == false)
                    {
                        if (topLevelOnly)
                        {
                            AddToChangeList(addToList, propertyValueCollection, auditPropertyInfo.Name, oldValue, newValue);
                        }

                        return false;
                    }
                }

                // Check of "CommonLanguageRuntimeLibrary" is needed because string is also a class
                if (auditPropertyInfo.PropertyType.IsClass &&
                   !auditPropertyInfo.PropertyType.Module.ScopeName.Equals("CommonLanguageRuntimeLibrary"))
                {
                    // Since we are going deeper (Property of this instance and this property is not basic type and it is class) 
                    // we pass false  as we are not top level (Child class Property compare)
                    // as we interested only if the children are equal or not and not recording what realy has changed
                    if (DeepCompare(oldValue, newValue, false, null, null))
                    {
                        continue;
                    }

                    return false;
                }

                // Now we are checking the leaf values of any instance but if it is the top level object 
                // we want to inform ther caller to this method that the top level has changed
                if (oldValue != newValue && (oldValue == null || !oldValue.Equals(newValue)))
                {
                    if (topLevelOnly)
                    {
                        AddToChangeList(addToList, propertyValueCollection, auditPropertyInfo.Name, oldValue, newValue);
                    }

                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, object> ExtractValues<T>(T instance,
                                                                    List<string> interestedProperties,
                                                                    List<PropertyInfo> propertyInfos)
            where T : class
        {
            // This is where we have to get some values out from audit instance apart from minimum 
            // ComparePropertyChange(ChangedParameter, ChangedParameterPrevValue, ChangedParameterNewValue)
            if (interestedProperties == null)
                interestedProperties = new List<string>();

            if (!interestedProperties.Contains("JournalType"))
                interestedProperties.Add("JournalType");

            // Apart from comapring what properties have their values change, we are also interested to get the value of some other if need be
            // So that they can be shown in the grid for use to view
            var listOfPropesAndVals = instance.GetPropertyValues(interestedProperties, propertyInfos);

            return listOfPropesAndVals;
        }

        private static void AddToChangeList(Action<Dictionary<string, object>, PropertyValueChange> addToListAction,
                                            Dictionary<string, object> interestedValues,
                                            string propertyName, object oldVal, object newVal)
        {
            // we are checking the main top level object to be added to list of changed instances
            // even though we comapre the composite objects too. at the end of the day we care for Top object in the list
            object oldValue = null;
            object newValue = null;
            object changedParameter = null;

            string changedParameterStatus;
            switch (Convert.ToString(interestedValues["JournalType"]))
            {
                case "I":
                    changedParameterStatus = "Added";
                    break;

                case "D":
                    changedParameterStatus = "Deleted";
                    break;

                default:
                    changedParameterStatus = "Changed";
                    oldValue = oldVal;
                    newValue = newVal;
                    changedParameter = propertyName;

                    break;
            }

            // This create the minimum amount of information class (base class) of changed properties
            // and what ever class that inherit from this will be filled using addToList Action<Dictionary<string, object>, ComparePropertyChange>  
            // As we get name and value of our interested properties in the code that call this method

            var propChange = new PropertyValueChange
            {
                ChangedParameter = (string)changedParameter,
                ChangedParameterPrevValue = oldValue,
                ChangedParameterNewValue = newValue,
                ChangedParameterStatus = changedParameterStatus
            };

            addToListAction(interestedValues, propChange);
        }



        /// <summary>
        /// Compare 2 collection that have IEnumerable Needs to be checked thoughrowly
        /// </summary>
        public static bool CheckForCollectionEquality(IEnumerable collection1, IEnumerable collection2)
        {
            var source = collection1.Cast<object>().ToList();
            var destination = collection2.Cast<object>().ToList();

            if (source.Count() != destination.Count())
            {
                return false;
            }

            var dictionary = new Dictionary<object, int>();

            foreach (var value in source)
            {
                if (!dictionary.ContainsKey(value))
                {
                    dictionary[value] = 1;
                }
                else
                {
                    dictionary[value]++;
                }
            }

            foreach (var member in destination)
            {
                if (!dictionary.ContainsKey(member))
                {
                    return false;
                }

                dictionary[member]--;
            }

            foreach (var kvp in dictionary)
            {
                if (kvp.Value != 0)
                {
                    return false;
                }
            }

            return true;
        }



        public static void Compare<TA, TC>(TA instanceTa, TC instanceTc, Action<PropertyValueChange> propertyValueChange)
            where TA : class
            where TC : class
        {
            object compareFromInstance;
            object compareToInstance;

            var compareFromType = instanceTa.GetType();
            var compareToType = instanceTc.GetType();

            string journalType;
            DateTime journalTimeStamp;
            string changedEntity;

            // If instanceTc (what we are comparing to) is also an audit instance like instanceTa
            // we bench mark instanceTc to get what is changed
            if (typeof(Audit).IsAssignableFrom(compareToType))
            {

                compareFromInstance = instanceTa;
                compareToInstance = instanceTc;

                journalType = (string)compareToInstance.GetPropertyValue("JournalType");
                journalTimeStamp = (DateTime)compareToInstance.GetPropertyValue("JournalTimeStamp");
                changedEntity = (string)compareToInstance.GetPropertyValue("EntityName");

                if (IsInsertOrDelete(compareToInstance, propertyValueChange))
                    return;

                // Get all propertyInfos for public properties if exist in the cache
                // And // Filter more and Get properties that dont have IgnoreCompareAttribute attribute as we are interested in them for comparing
                var cachedCompareTo = Cache.CheckCache(compareToType).ToList();
                var compareToPropertyInfos = cachedCompareTo.Where(p => !Attribute.IsDefined(p, typeof(IgnoreCompareAttribute)));

                var cachedCompareFrom = Cache.CheckCache(compareFromType).ToList();
                var compareFromPropertyInfos = cachedCompareFrom.Where(p => !Attribute.IsDefined(p, typeof(IgnoreCompareAttribute)))
                                                                .ToList();

                foreach (var compareToPropertyInfo in compareToPropertyInfos)
                {
                    // if this is a collection then it has tobe handles differently
                    if (compareToPropertyInfo.PropertyType != typeof(string) &&
                        typeof(IEnumerable).IsAssignableFrom(compareToPropertyInfo.PropertyType))
                    {
                        continue;
                    }

                    // Check of "CommonLanguageRuntimeLibrary" is needed because string is also a class
                    if (compareToPropertyInfo.PropertyType.IsClass &&
                       !compareToPropertyInfo.PropertyType.Module.ScopeName.Equals("CommonLanguageRuntimeLibrary"))
                    {
                        continue;
                    }

                    var compareFromPropertyInfo = compareFromPropertyInfos.SingleOrDefault(p => p.Name == compareToPropertyInfo.Name);
                    if (compareFromPropertyInfo != null)
                    {
                        // Older value
                        var compareFromValue = compareFromPropertyInfo.GetValue(compareFromInstance, null);
                        // And hence the new value
                        var compareToValue = compareToPropertyInfo.GetValue(compareToInstance, null);

                        // Now we are checking the leaf values of any instance but if it is the top level object 
                        // we want to inform ther caller to this method that the top level has changed
                        if (compareFromValue != compareToValue &&
                           (compareFromValue == null || !compareFromValue.Equals(compareToValue)))
                        {
                            AddToChangeList(propertyValueChange,
                                            journalType,
                                            journalTimeStamp,
                                            changedEntity,
                                            compareToPropertyInfo.Name, compareFromValue, compareToValue);
                        }
                    }

                }
            }
            else // compareToType is not an audit but actual object
            {
                compareFromInstance = instanceTa;
                compareToInstance = instanceTc;

                journalType = (string)compareFromInstance.GetPropertyValue("JournalType");
                journalTimeStamp = (DateTime)compareFromInstance.GetPropertyValue("JournalTimeStamp");
                changedEntity = (string)compareFromInstance.GetPropertyValue("EntityName");

                if (IsInsertOrDelete(compareFromInstance, propertyValueChange))
                    return;

                // Get all propertyInfos for public properties if exist in the cache
                var cachedcompareFromPropertyInfos = Cache.CheckCache(compareFromType).ToList();

                // Filter more and Get properties that dont have IgnoreCompareAttribute attribute as we are interested in them for comparing
                var compareFromPropertyInfos = cachedcompareFromPropertyInfos.Where(p => !Attribute.IsDefined(p, typeof(IgnoreCompareAttribute)));

                foreach (var compareFromPropertyInfo in compareFromPropertyInfos)
                {
                    // if this is a collection then it has tobe handles differently
                    if (compareFromPropertyInfo.PropertyType != typeof(string) &&
                        typeof(IEnumerable).IsAssignableFrom(compareFromPropertyInfo.PropertyType))
                    {
                        continue;
                    }

                    // Check of "CommonLanguageRuntimeLibrary" is needed because string is also a class
                    if (compareFromPropertyInfo.PropertyType.IsClass &&
                       !compareFromPropertyInfo.PropertyType.Module.ScopeName.Equals("CommonLanguageRuntimeLibrary"))
                    {
                        continue;
                    }

                    // Older value
                    var compareFromValue = compareFromPropertyInfo.GetValue(compareFromInstance, null);
                    // And hence the new value
                    var compareToValue = compareFromPropertyInfo.GetValue(compareToInstance, null);

                    // Now we are checking the leaf values of any instance but if it is the top level object 
                    // we want to inform ther caller to this method that the top level has changed
                    if (compareFromValue != compareToValue &&
                       (compareFromValue == null || !compareFromValue.Equals(compareToValue)))
                    {
                        AddToChangeList(propertyValueChange,
                                        journalType,
                                        journalTimeStamp,
                                        changedEntity,
                                        compareFromPropertyInfo.Name, compareFromValue, compareToValue);
                    }
                }
            }
        }

        private static void AddToChangeList(Action<PropertyValueChange> actionPropertyChange, string journalType, DateTime journalTimeStamp, string changedEntity, string propertyName, object oldVal, object newVal)
        {
            // we are checking the main top level object to be added to list of changed instances
            // even though we comapre the composite objects too. at the end of the day we care for Top object in the list

            string changedParameterStatus;
            switch (journalType)
            {
                case "D":
                    changedParameterStatus = "Deleted";
                    break;

                case "I":
                    changedParameterStatus = "Added";
                    break;

                default:
                    changedParameterStatus = "Changed";
                    break;
            }

            object oldValue = oldVal;
            object newValue = newVal;
            object changedParameter = propertyName;

            // This create the minimum amount of information class (base class) of changed properties
            // and what ever class that inherit from this will be filled using addToList Action<Dictionary<string, object>, ComparePropertyChange>  
            // As we get name and value of our interested properties in the code that call this method

            var propChange = new PropertyValueChange
            {
                ChangedParameter = (string)changedParameter,
                ChangedParameterPrevValue = oldValue,
                ChangedParameterNewValue = newValue,
                ChangedParameterStatus = changedParameterStatus,
                ChangedEntity = changedEntity,
                ChangedTimeStamp = journalTimeStamp
            };

            actionPropertyChange(propChange);
        }

        //public static bool IsInsertOrDelete<TA>(TA auditInstance, Action<PropertyValueChange> propertyValueChange)
        //    where TA : class
        //{

        //    // Get all propertyInfos for public properties if exist in the cache
        //    var journalType = (string)auditInstance.GetPropertyValue("JournalType");
        //    var journalTimeStamp = (DateTime)auditInstance.GetPropertyValue("JournalTimeStamp");
        //    var changedEntity = (string)auditInstance.GetPropertyValue("EntityName");

        //    if (!journalType.Equals("I") || !journalType.Equals("D"))
        //    {
        //        return false;
        //    }

        //    AddToChangeList(propertyValueChange, journalType, journalTimeStamp, changedEntity, null, null, null);
        //    return true;
        //}

        public static bool IsInsertOrDelete<TA>(TA auditInstance, Action<PropertyValueChange> propertyValueChange)
           where TA : class
        {
            var auditInstanceType = auditInstance.GetType();

            // Get all propertyInfos for public properties if exist in the cache
            var journalType = (string)auditInstance.GetPropertyValue("JournalType");
            var journalTimeStamp = (DateTime)auditInstance.GetPropertyValue("JournalTimeStamp");
            var changedEntity = (string)auditInstance.GetPropertyValue("EntityName");

            if (!journalType.Equals("I"))
            {
                return false;
            }

            // Get all propertyInfos for public properties if exist in the cache
            var cachedAuditInstancePropertyInfos = Cache.CheckCache(auditInstanceType).ToList();


            // Filter more and Get properties that dont have IgnoreCompareAttribute attribute as we are interested in them for comparing
            var cachedAuditInstancePropertyInfosFiltered = cachedAuditInstancePropertyInfos.Where(p =>
            {
                var propertyInfo = p;
                var defined = !Attribute.IsDefined(propertyInfo, typeof(IgnoreCompareAttribute));
                return defined;
            }
               ).ToList();

            // if nothing is filtered collection means there is nothing in the properties that we want to show
            // regardless of number of properties so we have one and on only entry for the whole object
            if (!cachedAuditInstancePropertyInfosFiltered.Any())
            {
                AddToChangeList(propertyValueChange,
                               journalType,
                               journalTimeStamp,
                               changedEntity,
                               null,
                               null,   // Created from nothing(null)
                               null);
            }
            else
            {
                // Onther wise for each property show an entry
                foreach (var propertyInfo in cachedAuditInstancePropertyInfosFiltered)
                {
                    // if this is a collection then it has tobe handles differently
                    if (propertyInfo.PropertyType != typeof(string) &&
                        typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType))
                    {
                        continue;
                    }

                    // Check of "CommonLanguageRuntimeLibrary" is needed because string is also a class
                    if (propertyInfo.PropertyType.IsClass &&
                       !propertyInfo.PropertyType.Module.ScopeName.Equals("CommonLanguageRuntimeLibrary"))
                    {
                        continue;
                    }

                    // Older value
                    var propertyInfoValue = propertyInfo.GetValue(auditInstance, null);

                    if (!propertyInfo.Name.EndsWith("Id"))
                    {
                        AddToChangeList(propertyValueChange,
                                   journalType,
                                   journalTimeStamp,
                                   changedEntity,
                                   propertyInfo.Name,
                                   null,   // Created from nothing(null)
                                   propertyInfoValue);
                    }
                }
            }

            return true;
        }
    }
}
