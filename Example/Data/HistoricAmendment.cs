using System;

namespace Puma.MDE.Data
{
    [Serializable]
    public class HistoricAmendment
    {
        public int Id { get; set; }
        public int InstrumentId { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime FixingDate { get; set; }
    }
}
