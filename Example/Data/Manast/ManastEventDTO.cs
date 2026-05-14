using System;

namespace Puma.MDE.Data.Manast
{
    internal class ManastEventDTO
    {
        public bool IsDeleted { get; internal set; }
        public DateTime ExecutionDate { get; internal set; }
    }
}