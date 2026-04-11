using System;
using System.Runtime.InteropServices;

namespace Puma.MDE
{
    public sealed partial class Engine
    {
        public void InfoException(string message, Exception ex)
        {
            Log.Info(ex, message);
        }

        public void ErrorException(string message, Exception ex)
        {
            Log.Error(ex, message);
        }

        public void WarnException(string message, Exception ex)
        {
            Log.Warn(ex, message);
        }

        public void FatalException(string message, Exception ex)
        {
            Log.Fatal(ex, message);
        }

        public void DebugException(string message, Exception ex)
        {
            Log.Debug(ex, message);
        }

        public void LogError(string message, Exception ex)
        {
            Log.Error(ex, message);
        }

        [ComVisible(false)]
        public void LogDebugArray(string name, double[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
                log.Debug(string.Format("{0}[{1}]={2}", name, i, arr[i]));
        }

        [ComVisible(false)]
        public void LogDebugArray(string name, int[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
                log.Debug(string.Format("{0}[{1}]={2}", name, i, arr[i]));
        }

        [ComVisible(false)]
        public void LogDebugArray(string name, long[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
                log.Debug(string.Format("{0}[{1}]={2}", name, i, arr[i]));
        }

        [ComVisible(false)]
        public void LogDebugValue(string name, object val)
        {
            log.Debug(string.Format("{0}={1}", name, val));
        }
    }
}
