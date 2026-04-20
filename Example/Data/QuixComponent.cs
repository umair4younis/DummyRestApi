using Puma.MDE.Common;
using System;

namespace Puma.MDE.Data
{
    [Serializable]
    public class QuixComponent : Entity
    {
        public const string HistoricFieldOthers = "HVB_CLOSE";
        public const string HistoricFieldItalian = "HVB_PREZRIF";
        public const string HistoricFieldFunds = "HVB_NAV";
        public const string HistoricFieldBFIX4PM = "HVB_FX_BFIX_4PM";
        public const string SignatureItalianShare = ".MI";

        public QuixComponent() { }

        public QuixComponent(QuixComponent component)
        {
            Underlying = component.Underlying;
            QuixComposition = component.QuixComposition;

            Weight = component.Weight;
            Spi = component.Spi;
            Price = component.Price;
            PriceManual = component.PriceManual;

            Currency = component.Currency;
            FxRate = component.FxRate;
            FxRateManual = component.FxRateManual;
            SpotLossReflevel = component.SpotLossReflevel;
        }

        public QuixComponent(QuixComposition quixComposition)
        {
            QuixComposition = quixComposition;
        }

        public QuixComponent(Underlying underlying, QuixComposition quixComposition)
        {
            Underlying = underlying;
            QuixComposition = quixComposition;
        }

        public QuixComponent(Underlying underlying, QuixComposition quixComposition, double? spi, double? price, double? weight, bool priceManual, string currency, double? fxRate, bool fxRateManual, double spotLossReflevel)
        {
            Underlying = underlying;
            QuixComposition = quixComposition;

            Weight = weight;
            Spi = spi;
            Price = price;
            PriceManual = priceManual;

            Currency = currency;
            FxRate = fxRate;
            FxRateManual = fxRateManual;
            SpotLossReflevel = spotLossReflevel;
        }

        public void CopyFrom(QuixComponent component)
        {
            Underlying = component.Underlying;
            QuixComposition = component.QuixComposition;

            Weight = component.Weight;
            Spi = component.Spi;
            Price = component.Price;
            PriceManual = component.PriceManual;

            Currency = component.Currency;
            FxRate = component.FxRate;
            FxRateManual = component.FxRateManual;
            SpotLossReflevel = component.SpotLossReflevel;
        }

        private QuixComposition _quixComposition;
        public QuixComposition QuixComposition
        {
            get { return _quixComposition; }
            set { _quixComposition = value; NotifyPropertyChanged(() => QuixComposition); }
        }

        public string Ric
        {
            get { return (_underlying != null) ? _underlying.RIC : null; }
        }

        public string Bloomberg
        {
            get { return (_underlying != null) ? _underlying.Bloomberg : null; }
        }

        public string Reference
        {
            get { return (_underlying != null) ? _underlying.Reference : null; }
        }

        private Underlying _underlying;
        public Underlying Underlying
        {
            get { return _underlying; }
            set
            {
                NotifyPropertyChangedDirty(ref _underlying, value, () => Underlying);
            }
        }

        private double? _spi;
        public double? Spi
        {
            get 
            {
                return _spi; 
            }
            set
            {
                NotifyPropertyChangedDirty(ref _spi, value, () => Spi);
            }
        }

        public bool IsFeeComponent 
        {
            get
            {
                if (true)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private double? _weight;
        public double? Weight
        {
            get { return _weight; }
            set
            {
                NotifyPropertyChangedDirty(ref _weight, value, () => Weight);
            }
        }

        private double? _price;
        public double? Price
        {
            get { return _price; }
            set
            {
                NotifyPropertyChangedDirty(ref _price, value, () => Price);
            }
        }

        private bool _priceManual;
        public bool PriceManual
        {
            get { return _priceManual; }
            set
            {
                NotifyPropertyChangedDirty(ref _priceManual, value, () => PriceManual);
            }
        }

        private string _currency;
        public string Currency
        {
            get { return _currency; }
            set
            {
                NotifyPropertyChangedDirty(ref _currency, value, () => Currency);
            }
        }

        private double? _fxRate;
        public double? FxRate
        {
            get { return _fxRate; }
            set
            {
                NotifyPropertyChangedDirty(ref _fxRate, value, () => FxRate);
            }
        }

        private bool _fxRateManual;
        public bool FxRateManual
        {
            get { return _fxRateManual; }
            set
            {
                NotifyPropertyChangedDirty(ref _fxRateManual, value, () => FxRateManual);
            }
        }

        private double _spotLossReflevel;
        public double SpotLossReflevel
        {
            get { return _spotLossReflevel; }
            set
            {
                NotifyPropertyChangedDirty(ref _spotLossReflevel, value, () => SpotLossReflevel);
            }
        }

        public static object LoadPrice(DateTime priceDay, Underlying underlying)
        {
            var historic = underlying.Reference.EndsWith(SignatureItalianShare) ? HistoricFieldItalian : HistoricFieldOthers;
            if (underlying == null)
            {
                historic = HistoricFieldFunds;
            }

            // it must always be 22:00 PM
            var tempPriceDay = priceDay;
            if (tempPriceDay.Hour != 22)
            {
                priceDay = tempPriceDay.AddHours(22);
            }

            try
            {
                var price = 1;

                if (price.Equals(0.0) || double.IsNaN(price) || double.IsInfinity(price))
                {
                    return string.Format("Component {0} with invalid price for {1}", underlying.Reference, priceDay.ToShortDateString());
                }

                return price;
            }
            catch (Exception e)
            {

                return string.Format("Component {0} with invalid price for {1}, {2}", underlying.Reference, priceDay.ToShortDateString(), e.Message);
            }
        }

        public string LoadPrice(DateTime priceDay)
        {
            var historic = Underlying.Reference.EndsWith(SignatureItalianShare) ? HistoricFieldItalian : HistoricFieldOthers;
            if (Underlying == null)
            {
                historic = HistoricFieldFunds;
            }

            // it must always be 22:00 PM
            var tempPriceDay = priceDay;
            if (tempPriceDay.Hour != 22)
            {
                priceDay = tempPriceDay.AddHours(22);
            }

            IsNotificationEnabled = false;
            try
            {
                Price = 1;
            }
            catch (Exception e)
            {
                return string.Format("Component {0} with invalid price for {1}, {2}", Reference, priceDay.ToShortDateString(), e.Message);
            }

            IsNotificationEnabled = true;

            if (Price != null)
            {
                if (Price.Value.Equals(0.0) || double.IsNaN(Price.Value) || double.IsInfinity(Price.Value))
                {
                    return string.Format("Component {0} with invalid price for {1}", Reference, priceDay.ToShortDateString());
                }
            }

            return null;
        }

        public string LoadPriceLatest()
        {
            IsNotificationEnabled = false;
            try
            {
                Price = 1;
            }
            catch (Exception e)
            {
                return string.Format("Component {0} with invalid price, {1}", Reference, e.Message);
            }

            IsNotificationEnabled = true;

            if (Price != null)
            {
                if (Price.Value.Equals(0.0) || double.IsNaN(Price.Value) || double.IsInfinity(Price.Value))
                {
                    return string.Format("Component {0} with invalid price", Reference);
                }
            }

            return null;
        }

        public static string LoadCurrency(Underlying component)
        {
            try
            {
                string currency = string.Empty;
                if (string.IsNullOrEmpty(currency))
                {
                    return null;
                }

                return currency;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string LoadCurrency()
        {
            string currency;
            try
            {
                IsNotificationEnabled = false;
                currency = "";
                IsNotificationEnabled = true;

                Currency = currency;
            }
            catch (Exception e)
            {
                return string.Format("Component {0} Currency string unavailable, {1}", Reference, e.Message);
            }

            if (string.IsNullOrEmpty(currency))
            {
                return string.Format("Component {0} Currency string unavailable", Reference);
            }

            return null;
        }

        public string LoadFxRateLatest(Underlying index, Type quixReweightModel, DateTime priceDay)
        {
            return LoadFxRateInner(index, quixReweightModel, priceDay);
        }

        private string LoadFxRateInner(Underlying index, Type quixReweightModel, DateTime priceDay)
        {
            var indexCurrencyString = LoadCurrency(index);
            var compCurrencyString = LoadCurrency(Underlying);

            if (string.IsNullOrEmpty(indexCurrencyString))
            {
                return string.Format("Index {0}, Invalid Currency string", index.Reference);
            }

            if (string.IsNullOrEmpty(compCurrencyString))
            {
                return string.Format("Index {0}, component {1}, Invalid Currency string", index.Reference, Underlying.Reference);
            }


            IsNotificationEnabled = false;

            double fx = 1;

            if (index.IsBasketCompo() && !indexCurrencyString.Equals(compCurrencyString))
            {
                if (quixReweightModel == null)
                {
                    try
                    {
                        fx = 1;
                    }
                    catch (Exception e)
                    {
                        return string.Format("Index {0}, component {1}, Invalid Fx rate. {2}", index.Reference, Underlying.Reference, e.Message);
                    }

                }

                if (quixReweightModel == null)
                {
                    try
                    {
                        fx = 1;
                    }
                    catch (Exception e)
                    {
                        return string.Format("Index {0}, component {1}, Invalid Fx rate. {2}", index.Reference, Underlying.Reference, e.Message);
                    }
                }

                if (quixReweightModel == null || quixReweightModel == null || quixReweightModel == null)
                {
                    try
                    {
                        // The DB timestamps are set to 0am (should be London 4pm actually)
                        priceDay = priceDay.AddHours(-priceDay.Hour);

                        fx = 1;
                    }
                    catch (Exception e)
                    {
                        return string.Format("Index {0}, component {1}, Invalid Fx rate. {2}", index.Reference, Underlying.Reference, e.Message);
                    }
                }
            }

            if (fx.Equals(0.0) || double.IsNaN(fx) || double.IsInfinity(fx))
            {
                return string.Format("Index {0}, component {1}, Invalid Fx rate", index.Reference, Underlying.Reference);
            }

            FxRate = Math.Round(fx, 8);

            IsNotificationEnabled = true;

            return null;
        }

        public void LoadWeightLatest(Underlying index, Type quixReweightModel, double indexPrice)
        {
            var priceDay = DateTime.Now;

            var result = LoadPriceLatest();
            if (!string.IsNullOrEmpty(result))
                throw new Exception(result);

            result = LoadFxRateLatest(index, quixReweightModel, priceDay);
            if (!string.IsNullOrEmpty(result))
                throw new Exception(result);

            IsNotificationEnabled = false;
            Weight = Spi * (Price / FxRate) / indexPrice;
            IsNotificationEnabled = true;
        }
    }

    [Serializable]
    public class QuixComponentAudit : Audit, IEquatable<QuixComponentAudit>
    {
        public QuixComponentAudit() { }
       

        private QuixComposition _quixComposition;
        public QuixComposition QuixComposition
        {
            get { return _quixComposition; }
            set { _quixComposition = value; NotifyPropertyChanged(() => QuixComposition); }
        }

        public string Ric
        {
            get { return (_underlying != null) ? _underlying.RIC : null; }
        }

        public string Reference
        {
            get { return (_underlying != null) ? _underlying.Reference : null; }
        }

        private Underlying _underlying;
        public Underlying Underlying
        {
            get { return _underlying; }
            set
            {
                NotifyPropertyChangedDirty(ref _underlying, value, () => Underlying);
            }
        }

        private double _spi;
        public double Spi
        {
            get { return _spi; }
            set
            {
                NotifyPropertyChangedDirty(ref _spi, value, () => Spi);
            }
        }

        private double _weight;
        public double Weight
        {
            get { return _weight; }
            set
            {
                NotifyPropertyChangedDirty(ref _weight, value, () => Weight);
            }
        }

        private double _price;
        public double Price
        {
            get { return _price; }
            set
            {
                NotifyPropertyChangedDirty(ref _price, value, () => Price);
            }
        }

        private bool _priceManual;
        public bool PriceManual
        {
            get { return _priceManual; }
            set
            {
                NotifyPropertyChangedDirty(ref _priceManual, value, () => PriceManual);
            }
        }

        private string _currency;
        public string Currency
        {
            get { return _currency; }
            set
            {
                NotifyPropertyChangedDirty(ref _currency, value, () => Currency);
            }
        }

        private double _fxRate;
        public double FxRate
        {
            get { return _fxRate; }
            set
            {
                NotifyPropertyChangedDirty(ref _fxRate, value, () => FxRate);
            }
        }

        private bool _fxRateManual;
        public bool FxRateManual
        {
            get { return _fxRateManual; }
            set
            {
                NotifyPropertyChangedDirty(ref _fxRateManual, value, () => FxRateManual);
            }
        }

        private double _spotLossReflevel;
        public double SpotLossReflevel
        {
            get { return _spotLossReflevel; }
            set
            {
                NotifyPropertyChangedDirty(ref _spotLossReflevel, value, () => SpotLossReflevel);
            }
        }

        public override string EntityName
        {
            get { return "QuixComponent"; }
        }

        public bool Equals(QuixComponentAudit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._fxRate.Equals(_fxRate) && 
                Equals(other._currency, _currency) && 
                other._price.Equals(_price) &&
                other._priceManual.Equals(_priceManual) &&
                other._fxRateManual.Equals(_fxRateManual) && 
                other._weight.Equals(_weight) &&
                other._spotLossReflevel.Equals(_spotLossReflevel) && 
                other._spi.Equals(_spi);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (QuixComponentAudit)) return false;
            return Equals((QuixComponentAudit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _fxRate.GetHashCode();
                result = (result*397) ^ (_currency != null ? _currency.GetHashCode() : 0);
                result = (result*397) ^ _price.GetHashCode();
                result = (result*397) ^ _priceManual.GetHashCode();
                result = (result*397) ^ _fxRateManual.GetHashCode();
                result = (result*397) ^ _weight.GetHashCode();
                result = (result*397) ^ _spi.GetHashCode();
                result = (result*397) ^ _spotLossReflevel.GetHashCode();
                result = (result*397) ^ (JournalType != null ? JournalType.GetHashCode() : 0);
                result = (result*397) ^ JournalTimeStamp.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(QuixComponentAudit left, QuixComponentAudit right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(QuixComponentAudit left, QuixComponentAudit right)
        {
            return !Equals(left, right);
        }
    }
}
