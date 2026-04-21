using System;


namespace Puma.MDE.OPUS.OrderImport
{
    public class DownloadedOrderFile
    {
        public string FileName { get; set; }

        public string RemotePath { get; set; }

        public byte[] Content { get; set; }

        public DateTime? LastWriteTimeUtc { get; set; }
    }
}