using System;

namespace Puma.MDE.Data
{
    public class HVBMassInterpolationWindow
    {
        private String interpolationWindow;

        public String InterpolationWindow
        {
            get { return interpolationWindow; }
            set { interpolationWindow = value; }
        }

        private string windowSize;

        public string WindowSize
        {
            get { return windowSize; }
            set { windowSize = value; }
        }

        public bool isDirty;
        public bool IsDirty
        {
            get;
            set;
        }
    }
}
