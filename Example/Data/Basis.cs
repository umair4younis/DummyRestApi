using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("7CD67CA3-FA9E-4c47-9816-2D336E2CA484")]
    public class Basis
    {
        public Basis()
        {
            UnderlyingId = 0;
            Timestamp = DateTime.Now;
            BasisValue = 0;
        }

        public Basis(Underlying underlying)
        {
            UnderlyingId = underlying.Id;
            Timestamp = DateTime.Now;
            BasisValue = 0;
        }

        public int Id { get; set; }
        public int UnderlyingId { get; set; }
        public double BasisValue { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
