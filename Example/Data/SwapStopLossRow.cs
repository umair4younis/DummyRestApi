using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapStopLossRow : Entity
    {
        public SwapStopLossRow() { }
        public SwapStopLossRow(string dbName, int accountId, SwapStopLossKindRow kind, double pct, DateTime xDate)
        {
            this.DbName = dbName;
            this.AccountId = accountId;
            this.Kind = kind;
            this.Pct = pct;
            this.xDate = xDate;
        }
        public string DbName { get; set; }
        public int AccountId { get; set; }
        public int? KindId { get; set; }
        public SwapStopLossKindRow Kind
        {
            get
            {
                if (KindId.HasValue)
                {
                    return new SwapStopLossKindRow();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value != null)
                {
                    KindId = value.Id;
                }
                else
                {
                    KindId = null;
                }
                NotifyPropertyChanged(nameof(Kind));
                NotifyPropertyChanged(nameof(KindName));
            }
        }
        public string KindName
        {
            get => Kind != null ? Kind.xName : string.Empty;
            set
            {
                var kindRow = value;
                if (kindRow == null)
                {
                    Kind = null;
                }
                NotifyPropertyChanged(() => Kind);
                NotifyPropertyChanged(() => KindName);
            }
        }
        public double Pct { get; set; }
        public DateTime xDate { get; set; }

        public string mIndexDate { get; set; } = DateTime.MinValue.ToString("dd/MM/yyyy");
        public double mIndexValue { get; set; } = 0.0;
        public double mPerformance { get; set; } = 0.0;

        public XStopLossData getData()
        {
            return new XStopLossData(Kind.xName, Pct, xDate);
        }
    }
}
