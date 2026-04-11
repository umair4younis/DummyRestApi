using System;

namespace Puma.MDE
{
    public sealed partial class Engine
    {
        public string GetHistomvtsName()
        {
#if SOPHIS_7
            return "JOIN_POSITION_HISTOMVTS";
#else
            return "HISTOMVTS";
#endif
        }

        public DateTime Today { get; set; }

        static System.Reflection.Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            System.Reflection.Assembly a = null;

            string serializationAssemblyPartialName = "PumaMDE";
            string serializationAssemblyPartialNameWinUI = "PumaMDEWinUI";
            string assemblyPartialNameHibernate = "nhibernate,";

            if (args.Name.IndexOf(assemblyPartialNameHibernate, StringComparison.InvariantCultureIgnoreCase) != -1)
                return System.Reflection.Assembly.GetAssembly(typeof(NHibernate.ISession));

            if (args.Name.IndexOf(serializationAssemblyPartialName, StringComparison.InvariantCultureIgnoreCase) != -1 &&
               args.Name.IndexOf(serializationAssemblyPartialNameWinUI, StringComparison.InvariantCultureIgnoreCase) == -1)
            {
                return System.Reflection.Assembly.GetExecutingAssembly();
            }

            return a;
        }
    }
}