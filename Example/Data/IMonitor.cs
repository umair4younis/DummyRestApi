using System;

namespace Puma.MDE.Data
{
    public interface IMonitor
    {
        string Workset { get; }
        string UnderlyingReference { get; set; }
        string Type { get; }
        DateTime CheckedAt { get; set; }
        DateTime PublishedAt { get; set; }
        TimeSpan StartOffset { get; set; }
        TimeSpan EndOffset { get; set; }
    }
}
