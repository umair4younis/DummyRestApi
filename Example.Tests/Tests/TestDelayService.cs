using System;
using System.Threading.Tasks;


namespace Puma.MDE.OPUS
{
    public static class TestDelayService
    {
        public static Func<int, Task> DelayFunc { get; set; } = Task.Delay;

        public static Task Delay(int milliseconds)
        {
            return DelayFunc(milliseconds);
        }
    }
}
