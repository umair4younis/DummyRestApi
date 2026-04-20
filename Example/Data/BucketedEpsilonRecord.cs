using System;
using System.ComponentModel;

namespace Puma.MDE.Data
{
    public class BucketedEpsilonRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        long id;
        public long Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                NotifyPropertyChanged("Id");
            }
        }

        int _SophisId;
        public int SophisId
        {
            get
            {
                return _SophisId;
            }
            set
            {
                _SophisId = value;
                NotifyPropertyChanged("SophisId");
            }
        }

        string _Portfolio;
        public string Portfolio
        {
            get
            {
                return _Portfolio;
            }
            set
            {
                _Portfolio = value;
                NotifyPropertyChanged("Portfolio");
            }
        }

        string _Bucket;
        public string Bucket
        {
            get
            {
                return _Bucket;
            }
            set
            {
                _Bucket = value;
                NotifyPropertyChanged("Bucket");
            }
        }

        DateTime? _BucketDate;
        public DateTime? BucketDate
        {
            get
            {
                return _BucketDate;
            }
            set
            {
                _BucketDate = value;
                NotifyPropertyChanged("BucketDate");
            }
        }

        decimal _Epsilon;
        public decimal Epsilon
        {
            get
            {
                return _Epsilon;
            }
            set
            {
                _Epsilon = value;
                NotifyPropertyChanged("Epsilon");
            }
        }
    }
}
