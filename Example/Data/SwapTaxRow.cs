using Puma.MDE.Common;
using System;

namespace Puma.MDE.Data
{
    public class SwapTaxRow : Entity
    {
		public string DbName {  get; set; }
		public string TaxType { get; set; }
		public string Country {  get; set; }
		public DateTime DateFrom {  get; set; }
		public DateTime DateTo {  get; set; }
		public double TaxRate {  get; set; }
    }
}
