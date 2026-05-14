namespace Puma.MDE.Data.Manast
{
    internal class ManastInstrumentTypeDTO
    {
        public bool IsInventory { get; internal set; }
        public bool IsFXSpot { get; internal set; }
        public bool IsBond { get; internal set; }
        public bool IsCash { get; internal set; }
    }
}