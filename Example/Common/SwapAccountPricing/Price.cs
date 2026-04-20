using System;
using System.Collections.Generic;
using Puma.MDE.Data;

namespace Puma.MDE.SwapAccountPricing
{
    public class SPriceRequest
    {
        public SPriceRequest() : this(TypeManastPriceField.eNoPriceField, TypeManastCloseRefDate.eNoRefDate, null) { }

        public SPriceRequest(TypeManastPriceField field) : this(field, TypeManastCloseRefDate.eNoRefDate, null) { }

        public SPriceRequest(TypeManastPriceField field, TypeManastCloseRefDate refDate, DateTime? closeDate)
        {
            mField = field;
            mRefDate = refDate;
            mCloseDate = closeDate;
        }

        public TypeManastPriceField mField; // eg. "Fixing1"
        public TypeManastCloseRefDate mRefDate; // eg. "T"
        public DateTime? mCloseDate; // filled only when mRefDate = "Specific"

        public override string ToString()
        {
            return string.Format("{0} on {1}", mField.ToString().Substring(1), mRefDate.ToString().Substring(1)); // eg. "Fixing1 on T"
        }
    }

	public class Price
	{
        public Price() : this(double.NaN, DateTime.MinValue, null, TypeManastPriceField.eNoPriceField) { }

        public Price(Price source) : this(source.Value, source.Updated, source.RequestType, source.ReturnField) { } // copy constructor

        public Price(Price source, SPriceRequest requestType, TypeManastPriceField returnField) : this(source.Value, source.Updated, requestType, returnField) { }

        public Price(double value, DateTime updated) : this(value, updated, null, TypeManastPriceField.eNoPriceField) { }

        public Price(double value, DateTime updated, SPriceRequest requestType, TypeManastPriceField returnField)
		{
			Value = value;
            Updated = updated;
            RequestType = requestType;
            ReturnField = returnField;
		}

        public double Value { get; set; }
        public DateTime Updated { get; set; }
        public SPriceRequest RequestType { get; set; } // the price type that is requested
        public TypeManastPriceField ReturnField { get; set; } // the PriceField that Value is taken from

        public class SortPrice : IComparer<Price>
        {
            public int Compare(Price left, Price right)
            {
                return left.Updated.CompareTo(right.Updated);
            }
        };

        public override string ToString()
        {
            return string.Format("Price:{0}, Time:{1}", Value, Updated);
        }
	}
}
