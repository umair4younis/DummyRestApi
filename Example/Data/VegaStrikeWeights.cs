using Puma.MDE.Common;
using System;
using System.Globalization;

namespace Puma.MDE.Data
{
    public class VegaStrikeWeights : Entity
    {
        public VegaStrikeWeights() { }
        ~VegaStrikeWeights() { }

        private decimal _iniWeight05;
        public decimal IniWeight05
        {
            get { return _iniWeight05; }
            set { _iniWeight05 = value; NotifyPropertyChanged(() => IniWeight05); }
        }

        private decimal _iniWeight08;
        public decimal IniWeight08
        {
            get { return _iniWeight08; }
            set { _iniWeight08 = value; NotifyPropertyChanged(() => IniWeight08); }
        }

        private decimal _iniWeight10;
        public decimal IniWeight10
        {
            get { return _iniWeight10; }
            set { _iniWeight10 = value; NotifyPropertyChanged(() => IniWeight10); }
        }

        private decimal _iniWeight12;
        public decimal IniWeight12
        {
            get { return _iniWeight12; }
            set { _iniWeight12 = value; NotifyPropertyChanged(() => IniWeight12); }
        }

        private decimal _iniWeight15;
        public decimal IniWeight15
        {
            get { return _iniWeight15; }
            set { _iniWeight15 = value; NotifyPropertyChanged(() => IniWeight15); }
        }

        private decimal _finWeight05;
        public decimal FinWeight05
        {
            get { return _finWeight05; }
            set { _finWeight05 = value; NotifyPropertyChanged(() => FinWeight05); }
        }

        private decimal _finWeight08;
        public decimal FinWeight08
        {
            get { return _finWeight08; }
            set { _finWeight08 = value; NotifyPropertyChanged(() => FinWeight08); }
        }

        private decimal _finWeight10;
        public decimal FinWeight10
        {
            get { return _finWeight10; }
            set { _finWeight10 = value; NotifyPropertyChanged(() => FinWeight10); }
        }

        private decimal _finWeight12;
        public decimal FinWeight12
        {
            get { return _finWeight12; }
            set { _finWeight12 = value; NotifyPropertyChanged(() => FinWeight12); }
        }

        private decimal _finWeight15;
        public decimal FinWeight15
        {
            get { return _finWeight15; }
            set { _finWeight15 = value; NotifyPropertyChanged(() => FinWeight15); }
        }

        private decimal _years;
        public decimal Years
        {
            get { return _years; }
            set { _years = value; NotifyPropertyChanged(() => Years); }
        }

        public VegaStrikeWeights Clone()
        {
            VegaStrikeWeights retval = new VegaStrikeWeights();

            retval.IniWeight05 = IniWeight05;
            retval.IniWeight08 = IniWeight08;
            retval.IniWeight10 = IniWeight10;
            retval.IniWeight12 = IniWeight12;
            retval.IniWeight15 = IniWeight15;
            retval.FinWeight05   = FinWeight05;
            retval.FinWeight08   = FinWeight08;
            retval.FinWeight10   = FinWeight10;
            retval.FinWeight12   = FinWeight12;
            retval.FinWeight15   = FinWeight15;
            retval.Years           = Years;
            return retval;
        }


        public bool Equals(VegaStrikeWeights other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.IniWeight05 == IniWeight05 &&
                   other.IniWeight08 == IniWeight08 &&
                   other.IniWeight10 == IniWeight10 &&
                   other.IniWeight12 == IniWeight12 &&
                   other.IniWeight15 == IniWeight15 &&
                   other.FinWeight05   == FinWeight05 &&
                   other.FinWeight08   == FinWeight08 &&
                   other.FinWeight10   == FinWeight10 &&
                   other.FinWeight12   == FinWeight12 &&
                   other.FinWeight15   == FinWeight15 &&
                   other.Years == Years;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(VegaStrikeWeights)) return false;
            return Equals((VegaStrikeWeights)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (IniWeight05.GetHashCode() * 397) ^ FinWeight05.GetHashCode();
            }
        }


        public override string ToString()
        {
            return String.Format("VegaStrikeWeights<" +
                                 "IniWeight05: {0}, " +
                                 "IniWeight08: {1}, " +
                                 "IniWeight10: {2}, " +
                                 "IniWeight12: {3}, " +
                                 "IniWeight15: {4}, " +
                                 "FinWeight05: {5}, " +
                                 "FinWeight08: {6}, " +
                                 "FinWeight10: {7}, " +
                                 "FinWeight12: {8}, " +
                                 "FinWeight15: {9}, " +
                                 "Years: {10}>",
                                 IniWeight05.ToString("F", CultureInfo.InvariantCulture),
                                 IniWeight08.ToString("F", CultureInfo.InvariantCulture),
                                 IniWeight10.ToString("F", CultureInfo.InvariantCulture),
                                 IniWeight12.ToString("F", CultureInfo.InvariantCulture),
                                 IniWeight15.ToString("F", CultureInfo.InvariantCulture),
                                 FinWeight05.ToString("F", CultureInfo.InvariantCulture),
                                 FinWeight08.ToString("F", CultureInfo.InvariantCulture),
                                 FinWeight10.ToString("F", CultureInfo.InvariantCulture),
                                 FinWeight12.ToString("F", CultureInfo.InvariantCulture),
                                 FinWeight15.ToString("F", CultureInfo.InvariantCulture),
                                 Years.ToString("F", CultureInfo.InvariantCulture));
        }
    }
}