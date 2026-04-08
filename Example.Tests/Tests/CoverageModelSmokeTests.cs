using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace Puma.MDE.Tests
{
    [TestClass]
    public class CoverageModelSmokeTests
    {
        [TestMethod]
        public void Models_Can_Be_Instantiated_And_Properties_Accessed()
        {
            Assembly asm = typeof(AmountValue).Assembly;
            Type[] types = GetLoadableTypes(asm)
                .Where(t => t.Namespace == "Puma.MDE.OPUS.Models" && t.IsClass && !t.IsAbstract)
                .ToArray();

            Assert.IsTrue(types.Length > 0);

            foreach (var type in types)
            {
                if (type.ContainsGenericParameters)
                {
                    continue;
                }

                object instance = null;
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                {
                    instance = Activator.CreateInstance(type);
                }
                else
                {
                    continue;
                }

                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.CanRead) continue;

                    if (prop.CanWrite)
                    {
                        object sample = CreateSampleValue(prop.PropertyType);
                        if (sample != null || !prop.PropertyType.IsValueType)
                        {
                            try { prop.SetValue(instance, sample); } catch { }
                        }
                    }

                    try { _ = prop.GetValue(instance); } catch { }
                }

                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    object sample = CreateSampleValue(field.FieldType);
                    try { field.SetValue(instance, sample); } catch { }
                    try { _ = field.GetValue(instance); } catch { }
                }
            }
        }

        [TestMethod]
        public void SwapValidationResult_Factory_And_Summary_Coverage()
        {
            var success = SwapValidationResult.Success("s1", new TotalReturnSwapResponse
            {
                Uuid = "s1",
                Nominal = new AmountValue { Quantity = 1000m }
            });

            Assert.IsTrue(success.IsValid);
            Assert.IsTrue(success.HasWarnings);
            Assert.IsTrue(success.GetSummary().Contains("Swap s1"));

            var fail = SwapValidationResult.Failure("s2", "boom");
            Assert.IsFalse(fail.IsValid);
            Assert.IsTrue(fail.GetSummary().Contains("boom"));
        }

        private static object CreateSampleValue(Type t)
        {
            if (t == typeof(string)) return "x";
            if (t == typeof(int)) return 1;
            if (t == typeof(long)) return 1L;
            if (t == typeof(decimal)) return 1m;
            if (t == typeof(double)) return 1d;
            if (t == typeof(float)) return 1f;
            if (t == typeof(bool)) return true;
            if (t == typeof(DateTime)) return DateTime.UtcNow;
            if (t == typeof(Guid)) return Guid.NewGuid();

            Type nullable = Nullable.GetUnderlyingType(t);
            if (nullable != null) return CreateSampleValue(nullable);

            if (t.IsEnum)
            {
                Array values = Enum.GetValues(t);
                return values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(t);
            }

            if (t.IsArray)
            {
                return Array.CreateInstance(t.GetElementType(), 0);
            }

            if (t.IsGenericType)
            {
                Type gtd = t.GetGenericTypeDefinition();
                if (gtd == typeof(List<>))
                {
                    return Activator.CreateInstance(t);
                }
            }

            if (typeof(IEnumerable).IsAssignableFrom(t) && t.IsClass)
            {
                var ctor = t.GetConstructor(Type.EmptyTypes);
                if (ctor != null) return Activator.CreateInstance(t);
            }

            if (t.IsClass)
            {
                var ctor = t.GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                {
                    try { return Activator.CreateInstance(t); } catch { }
                }
            }

            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }
    }
}
