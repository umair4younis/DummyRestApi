using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    public interface IHasValue
    {
        double Value { get; set; }
    }

    [ComVisible(true)]
    [Serializable]
    public class SwapManagementFee1 : Entity, IHasValue
    {
        public SwapManagementFee1() { }
        public SwapManagementFee1(string DbName, int AccountId, double value, double feePct)
        {
            this.DbName = DbName;
            this.AccountId = AccountId;
            this.Value = value;
            this.FeePct = feePct;
        }
        public String DbName { get; set; }
        public int AccountId { get; set; }
        public virtual double Value { get; set; }
        public double FeePct { get; set; }
    }

    [ComVisible(true)]
    [Serializable]
    public class SwapManagementFee2 : Entity, IHasValue
    {
        public SwapManagementFee2() { }
        public SwapManagementFee2(string DbName, int AccountId, double value, double feePct)
        {
            this.DbName = DbName;
            this.AccountId = AccountId;
            this.Value = value;
            this.FeePct = feePct;
        }
        public String DbName { get; set; }
        public int AccountId { get; set; }
        public virtual double Value { get; set; }
        public double FeePct { get; set; }
    }

    [ComVisible(true)]
    [Serializable]
    public class SwapManagementFee3 : Entity, IHasValue
    {
        public SwapManagementFee3() { }
        public SwapManagementFee3(string DbName, int AccountId, double value, double feePct)
        {
            this.DbName = DbName;
            this.AccountId = AccountId;
            this.Value = value;
            this.FeePct = feePct;
        }
        public String DbName { get; set; }
        public int AccountId { get; set; }
        public virtual double Value { get; set; }
        public double FeePct { get; set; }
    }
}
