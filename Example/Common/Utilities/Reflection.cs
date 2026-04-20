using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Puma.MDE.Common.Utilities
{
    public static class Reflection
    {
        public static List<PropertyInfo> GetPropertyInfos(this object instance)
        {
            var instanceType = instance.GetType();
            var propertyInfos = instanceType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

            return propertyInfos;
        }

        public static List<PropertyInfo> GetPropertyInfosOnlyThisInstance(this object instance)
        {
            var instanceType = instance.GetType();
            var propertyInfos = instanceType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();

            return propertyInfos;
        }

        public static PropertyInfo GetPropertyInfo(this object instance, string propertyName)
        {
            var instanceType = instance.GetType();
            var propertyInfo = instanceType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            return propertyInfo;
        }

        public static PropertyInfo[] GetPropertyInfos(this Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy
                | BindingFlags.Public | BindingFlags.Instance);
        }

        public static object GetPropertyValue(this object instance, string propertyName)
        {
            var propertyInfos = instance.GetPropertyInfos();

            var propInfo = propertyInfos.FirstOrDefault(p => p.Name == propertyName);
            
            return propInfo == null ? null : propInfo.GetValue(instance, null);
        }

        public static Dictionary<string, object> GetPropertyValues(this object instance, List<string> interestedProperties)
        {
            var instanceType = instance.GetType();
            var propertyInfos = instanceType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

            return instance.GetPropertyValues(interestedProperties, propertyInfos);
        }

        public static Dictionary<string, object> GetPropertyValues(this object instance, 
                                                                    List<string> interestedProperties, 
                                                                    List<PropertyInfo> propertyInfos)
        {
            var listOfPropesAndVals = new Dictionary<string, object>();

            foreach (var propertyName in interestedProperties)
            {
                var propName = propertyName;
                var propInfo = propertyInfos.FirstOrDefault(p => p.Name == propName);

                if (propInfo == null)
                {
                    listOfPropesAndVals.Add(propertyName, null);
                    continue;
                }

                var value = propInfo.GetValue(instance, null);

                listOfPropesAndVals.Add(propertyName, value);
            }

            return listOfPropesAndVals;
        }

        
    }
}
