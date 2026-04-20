using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("08B53259-0423-4511-B453-01B71555C1B3")]
    public class Statistic
    {
        public Statistic()
        {
            Timestamp = DateTime.Now;
        }

        public Statistic(Underlying und, int userId)
        {
            Underlying = und.Reference;
            UserId = userId;
            Timestamp = DateTime.Now;
        }

        public int Id { get; set; }
        public String Underlying { get; set; }
        public DateTime Timestamp { get; set; }
        public int UserId { get; set; }
        public double QueryQuotes { get; set; }
        public double CalculateFilter { get; set; }
        public double StripSurface { get; set; }
        public double Publish { get; set; }
    }
}
