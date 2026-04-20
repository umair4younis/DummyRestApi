using Puma.MDE.Common;
using System;
using System.Collections.Generic;

namespace Puma.MDE.Data
{
    public class CalypsoCalendar : Entity
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public DateTime LastSophisUpdate { get; set; }
        public virtual IList<CalypsoCalendarHoliday> Holidays { get; set; }
    }

    public class CalypsoCalendarHoliday : Entity
    {
        public string Code { get; set; }
        public DateTime Date { get; set; }

        public override bool Equals(object other)
        {
            CalypsoCalendarHoliday otherHoliday = other as CalypsoCalendarHoliday;
            if (otherHoliday == null) return false;

            return Code == otherHoliday.Code && Date == otherHoliday.Date;
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            if (Code != null) hashCode += Code.GetHashCode() * 17;
            hashCode += Date.GetHashCode() * 34;
            return hashCode;
        }
    }
}
