using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Globalization;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("B9E0020C-00A4-4336-BAC5-15C0DF952C5D")]
    public class AutoATMThresholds : IEnumerable
    {
        IList<AutoATMThreshold> collection;
        public AutoATMThresholds(IList<AutoATMThreshold> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AutoATMThreshold item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AutoATMThreshold this[int index]
        {
            get
            {
                return collection[index];
            }
            set
            {
                collection[index] = value;
            }
        }
        public int Count
        {
            get
            {
                return collection.Count;
            }
        }

        public AutoATMThreshold ThresholdAt(DateTime Dt)
        {
            var orderedCollection = collection.OrderBy( x => x.Maturity );

            foreach (AutoATMThreshold threshold in orderedCollection)
            {
                if (threshold.Maturity >= Dt)
                    return threshold;
            }

            return orderedCollection.Last();
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("BB07946E-2583-46f5-A97E-BE5722B4C356")]
    public class AutoATMThreshold : INotifyPropertyChanged
    {
        
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public AutoATMThreshold()
        {
            ThresholdPublish = 0.01;
            ThresholdWarning = 0.01;
            Bucket = "1m";
            SkewThreshold = 0;
        }

        public int Id { get; set; }
        public String Bucket { get; set; }
        public DateTime Maturity
        {
            get
            {
                try
                {
                    return DateTime.ParseExact(Bucket, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    return Engine.Instance.Today;
                }
            }
            set
            {
                Bucket = value.ToString("yyyy-MM-dd");
            }
        }
        public double ThresholdPublish { get; set; }
        public double ThresholdWarning { get; set; }

        public double _ThresholdPublishSophis = 0;
        public double ThresholdPublishSophis 
        {
            get
            {
                if (_ThresholdPublishSophis <= 0)
                    return ThresholdPublish;
                
                return _ThresholdPublishSophis;
            }
            set
            {
                _ThresholdPublishSophis = value;
            }
        }

        double _AverageVolatilityThresholdWarning = 0;
        public double AverageVolatilityThresholdWarning 
        {
            get
            {
                if (_AverageVolatilityThresholdWarning == 0)
                    return ThresholdWarning*10;

                return _AverageVolatilityThresholdWarning;
            }
            set
            {
                _AverageVolatilityThresholdWarning = value;
            }
        }

        public double SkewThreshold { get; set; }

        double previousDayThreshold;
        public double PreviousDayThreshold
        {
            get
            {
                return previousDayThreshold;
            }
            set
            {
                previousDayThreshold = value;
                NotifyPropertyChanged("PreviousDayThreshold");
            }
        }

        public VolMonitor Monitor { get; set; }

        public AutoATMThreshold Clone()
        {
            AutoATMThreshold retval = new AutoATMThreshold();

            retval.Bucket = Bucket;
            retval.ThresholdPublish = ThresholdPublish;
            retval._ThresholdPublishSophis = _ThresholdPublishSophis;
            retval.ThresholdWarning = ThresholdWarning;
            retval._AverageVolatilityThresholdWarning = _AverageVolatilityThresholdWarning;
            retval.SkewThreshold = SkewThreshold;
            retval.PreviousDayThreshold = PreviousDayThreshold;

            return retval;
        }

    }
}
