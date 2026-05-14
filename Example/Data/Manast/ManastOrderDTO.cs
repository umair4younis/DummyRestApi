using System;

namespace Puma.MDE.Data.Manast
{
    public class ManastOrderDTO
    {
        public DateTime TradeDate { get; internal set; }
        public object DateExecuted { get; internal set; }
        public object DbName { get; internal set; }
        public object Type { get; internal set; }
        public TypeManastOrder OrderType { get; internal set; }
        public object Description { get; internal set; }
        public int AccountId { get; internal set; }
        public int UpdatePoolUpon { get; internal set; }

        internal bool IsDateExecutedNull()
        {
            throw new NotImplementedException();
        }
    }
}