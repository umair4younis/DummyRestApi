using System;

namespace Puma.MDE.Data
{
    public class CentralFixingsMonitor : IMonitor
    {
        private const string worksetMsg = "Central Fixings";
        private string _typeMessage;
        private string _underlyingReference;
        private DateTime _checkedAt;
        private DateTime _publishedAt;
        private TimeSpan _startOffset;
        private TimeSpan _endOffset;

        #region IMonitor Members
       
        public string Workset
        {            
            get
            {
                return worksetMsg;
            }            
        }

        public string UnderlyingReference
        {
            get
            {
                return _underlyingReference;
            }
            set
            {
                _underlyingReference = value;
            }
        }

        public string Type
        {
            set
            {
                _typeMessage = value;
            }
            get
            {
                return _typeMessage;
            }            
        }

        public DateTime CheckedAt
        {
            get
            {
                return _checkedAt;
            }
            set
            {
                _checkedAt = value;
            }
        }

        public DateTime PublishedAt
        {
            get
            {
                return _publishedAt;
            }
            set
            {
                _publishedAt = value;
            }
        }

        public TimeSpan StartOffset
        {
            get
            {
                return _startOffset;
            }
            set
            {
                _startOffset = value;
            }
        }

        public TimeSpan EndOffset
        {
            get
            {
                return _endOffset;
            }
            set
            {
                _endOffset = value;
            }
        }

        #endregion
    }
}
