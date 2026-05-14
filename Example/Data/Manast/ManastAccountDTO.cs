using System;

namespace Puma.MDE.Data.Manast
{
    public class ManastAccountDTO
    {
        internal object m_IncludeClosedPositions;
        internal double xCertificates;
        internal double xIndexValue;

        public int Id { get; internal set; }
        public string Currency { get; internal set; }
        public DateTime EventExecuted { get; internal set; }
        public object CreateDate { get; internal set; }
        public string AccountName { get; internal set; }
        public double IndexFactor { get; internal set; }
        public bool IsVirgin { get; internal set; }
    }
}