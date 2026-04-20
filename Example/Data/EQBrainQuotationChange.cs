using System;
using System.Collections.Generic;

namespace Puma.MDE.Data
{
    public class EQBrainQuotationChange
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }
        public string Username { get; set; }
        public int Status { get; set; }
        public long? ParentId { get; set; }
        public string ColumnName { get; set; }
        public int? DbId { get; set; }

        protected IList<EQBrainQuotationGroup> _QuotationGroup = new List<EQBrainQuotationGroup>();
        public IList<EQBrainQuotationGroup> QuotationGroup { get { return _QuotationGroup; } protected set { _QuotationGroup = value; } }
    }

    public class EQBrainQuotationGroup
    {
        public long Id { get; set; }
        public string Feedcode { get; set; }
        public string ValueAudit { get; set; }
        public string Value { get; set; }

        public EQBrainQuotationChange QuotationChange { get; set; }
    }
}
