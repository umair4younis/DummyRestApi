using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("18B84C43-7C99-417f-B928-E05D8888F612")]
    [ComVisible(true)]

    public class MaturitySchedule
    {
        public const string EuropeanTemplate = "Puma.MDE.MarketData.MaturityScheduleProviderEurope, PumaMDE";
        public const string AsianTemplate = "Puma.MDE.MarketData.MaturityScheduleProviderAsia, PumaMDE";
        public const string HongKongTemplate = "Puma.MDE.MarketData.MaturityScheduleProviderHongKong, PumaMDE";
        public const string FXTemplate = "Puma.MDE.MarketData.MaturityScheduleProviderFX, PumaMDE";


        public int Id { get; set; }
        public int ClassificationId { get; set; }
        public String Template { get; set; }

    }

    //public class FoMaturitySchedule : Audit
    //{
    //}

    //public class AuditMaturitySchedule : Audit
    //{
    //}

    //public class FoAuditMaturitySchedule : Audit
    //{
    //}

}
