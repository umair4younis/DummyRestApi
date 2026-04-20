using System;


namespace Puma.MDE.Data
{
    public class QuixCompositionCentralFixingMonitor
    {
        public string Workset { get; set; }
        public Underlying Underlying { get; set; }
        public Underlying IndexComponent { get; set; }
        public string Status { get; set; }
        public DateTime CheckedAt { get; set; }

        public DateTime PublishedAt { get; set; }

        public TimeSpan StartOffset { get; set; }
        public TimeSpan EndOffset { get; set; }

        public bool Alarm { get; set; }


        public QuixCompositionCentralFixingMonitor()
        {
        }

        public QuixCompositionCentralFixingMonitor(Underlying underlying, Underlying indexComponent)
        {
            Underlying = underlying;
            IndexComponent = indexComponent;
            Workset = "Quix CF Monitor";
            Status = "Central fixing amended for " + IndexComponent.Reference + ", is used for Quix index " + Underlying.Reference;

            PublishedAt = DateTime.MinValue;
            CheckedAt = DateTime.MinValue;

            Alarm = true;
        }
    }
}
