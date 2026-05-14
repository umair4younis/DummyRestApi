using System;

namespace Puma.MDE.Data.Manast
{
    internal class ManastInstrumentDTO
    {
        public string InstrumentName { get; internal set; }
        public object Id { get; internal set; }
        public string ISIN { get; internal set; }
        public object RIC { get; internal set; }

        internal bool IsISINNull()
        {
            throw new NotImplementedException();
        }

        internal bool IsRICNull()
        {
            throw new NotImplementedException();
        }
    }
}