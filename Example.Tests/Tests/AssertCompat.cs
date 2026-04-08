using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;


namespace Puma.MDE.Tests
{
    internal static class AssertCompat
    {
        public static T Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
                Assert.Fail("Expected exception of type " + typeof(T).FullName + " was not thrown.");
                return null;
            }
            catch (T ex)
            {
                return ex;
            }
        }

        public static async Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
        {
            try
            {
                await action();
                Assert.Fail("Expected exception of type " + typeof(T).FullName + " was not thrown.");
                return null;
            }
            catch (T ex)
            {
                return ex;
            }
        }
    }
}