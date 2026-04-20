namespace Puma.MDE.Data
{
    public class HVBMassSalesNationalFactor
    {
        public int Id           { get; set; }
        public long Notional    { get; set; }
        public long ClientId    { get; set; }
        public double Factor    { get; set; }
        public bool IsDirty     { get; set; }
    }
}
