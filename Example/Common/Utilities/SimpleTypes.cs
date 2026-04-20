using System;

namespace Puma.MDE.Common.Utilities
{
    public static class SimpleTypes
    {
        public static bool Equal(this double thisDouble, double anotherDouble)
        {
            return Math.Abs(thisDouble - anotherDouble) < double.Epsilon;
        }

        public static bool NotEqual(this double thisDouble, double anotherDouble)
        {
            return Math.Abs(thisDouble - anotherDouble) > double.Epsilon;
        }
    }
}
