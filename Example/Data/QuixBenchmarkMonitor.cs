using System;

namespace Puma.MDE.Data
{
    public class QuixBenchmarkMonitor
    {
        public string Workset { get; set; }
        public Underlying Underlying { get; set; }
        public Underlying Benchmark { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public DateTime CheckedAt { get; set; }

        public DateTime PublishedAt { get; set; }

        public TimeSpan StartOffset { get; set; }
        public TimeSpan EndOffset { get; set; }

        public bool Alarm { get; set; }

        public QuixBenchmarkMonitor()
        {
        }

        public QuixBenchmarkMonitor(Underlying underlying, Underlying benchmark)
        {
            Underlying = underlying;
            Benchmark = benchmark;
            Workset = "Quix Benchmarch Monitor";
            Type = string.Format("Index '{0}' has different composition than index '{1}', please check!", Benchmark.Reference, Underlying.Reference);

            PublishedAt = DateTime.MinValue;
            CheckedAt = DateTime.MinValue;

            Alarm = true;
        }
    }
}
