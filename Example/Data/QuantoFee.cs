using System;
using System.Collections.Generic;
using System.Linq;

using System.Runtime.InteropServices;
using System.ComponentModel;



namespace Puma.MDE.Data
{
    [ComVisible(false)]
    public class QuantoFee : INotifyPropertyChanged
    {
        static DateTime zeroDate = new DateTime(1904, 1, 1);
        const string ManagementFeeName = "Management Fee";
        public QuantoFee()
        {
            Frequency = "3m";
            Threshold = 10;
            ObservationStart = DateTime.Today;
            DeactivateUntilNext = false;
        }   

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

        string reference;
        public string Reference
        {
            get
            {
                return reference;
            }
            set
            {
                reference = value;
                NotifyPropertyChanged("Reference");
                NotifyPropertyChanged("Underlying");
                NotifyPropertyChanged("PaymentCcy");
                NotifyPropertyChanged("UsedQuantoFee");
                NotifyPropertyChanged("CalculatedQuantoFee");
                NotifyPropertyChanged("ManagementFee");
            }
        }

        public string Underlying
        {
            get
            {
                return String.Empty;
            }
        }

        double GetManagementFee(SR2COM.IInstrument ins)
        {
            double retVal = 0;
            try
            {
                var coms = ins.Compositions ;
                if ( coms!= null && coms.Count > 0)
                {
                    foreach (SR2COM.IComposition com in coms)
                    {
                        double one = GetManagementFee(com.Instrument);
                        if (one != 0)
                        {
                            retVal = one;
                            break;
                        }
                    }
                }
                else
                {
                    var fees = new List<KeyValuePair<DateTime, double>>();
                    foreach (SR2COM.IClause clause in ins.Clauses)
                    {
                        if (clause != null && clause.TypeName == ManagementFeeName)
                        {
                            if (clause.EndDate >= Engine.Instance.Today || clause.EndDate <= zeroDate)
                                fees.Add(new KeyValuePair<DateTime, double>((clause.EndDate <= zeroDate?DateTime.MaxValue:clause.EndDate), clause.Value));
                        }
                    }

                    if (fees.Any())
                        retVal = fees.OrderBy(x => x.Key).First().Value;
                }
            }
            catch
            {
            }
            return retVal;
        }

        public double ManagementFee
        {
            get
            {
                double retVal = 0;
                try
                {
                    SR2COM.IEngine engine = new SR2COM.Engine();
                    SR2COM.IInstrument ins = engine.get_Instrument(Reference);
                    retVal = GetManagementFee(ins);
                }
                catch
                {
                }
                return retVal;
            }
        }

        public string PaymentCcy
        {
            get
            {
                try
                {
                    SR2COM.IEngine engine = new SR2COM.Engine();
                    SR2COM.IInstrument ins = engine.get_Instrument(Reference);
                    return ins.SettlementCurrency.ID;
                }
                catch
                {
                }

                return String.Empty;
            }
        }

        double threshold;
        public double Threshold
        {
            get
            {
                return threshold;
            }
            set
            {
                threshold = value;
                NotifyPropertyChanged("Threshold");
            }
        }

        DateTime observationStart;
        public DateTime ObservationStart
        {
            get
            {
                return observationStart;
            }
            set
            {
                observationStart = value;
                NotifyPropertyChanged("ObservationStart");
            }
        }

        double additionalManagementFee;
        public double AdditionalManagementFee
        {
            get
            {
                return additionalManagementFee;
            }
            set
            {
                additionalManagementFee = value;
                NotifyPropertyChanged("AdditionalManagementFee");
                NotifyPropertyChanged("UsedQuantoFee");
                NotifyPropertyChanged("Warning");
            }
        }

        string frequency;
        public string Frequency
        {
            get
            {
                return frequency;
            }
            set
            {
                frequency = value;
                NotifyPropertyChanged("Frequency");
            }
        }

        bool deactivateUntilNext;
        public bool DeactivateUntilNext
        {
            get
            {
                return deactivateUntilNext;
            }
            set
            {
                deactivateUntilNext = value;
                NotifyPropertyChanged("DeactivateUntilNext");
                NotifyPropertyChanged("Warning");
            }
        }

        public double UsedQuantoFee
        {
            get
            {
                return ManagementFee - AdditionalManagementFee;
            }
        }
        double? calculatedQuantoFee;
        public double? CalculatedQuantoFee
        {
            get
            {
                if (calculatedQuantoFee.HasValue)
                    return calculatedQuantoFee;

                try
                {
                    double retVal = 0;
                    
                    SR2COM.IEngine engine = new SR2COM.Engine();
                    SR2COM.IInstrument ins = engine.get_Instrument(Underlying);

                    var oneYear = Engine.Instance.Today.AddYears(1);

                    var ccyUnd = ins.Currency;
                    var ccyPay = engine.get_CCy(PaymentCcy);
                    var insFx = engine.get_ForexInstrument(ccyUnd, ccyPay);

                    var correlation = ins.get_Correlation(insFx, Engine.Instance.Today, oneYear);
                    var fxvol = insFx.get_Volatility(Engine.Instance.Today, oneYear, 1, SR2COM.ENUMVOLATILITYTYPE.evResult, true);
                    var undvol = ins.get_Volatility(Engine.Instance.Today, oneYear, 1, SR2COM.ENUMVOLATILITYTYPE.evResult, true);

                    var insCcyRate = ccyUnd.get_Rate(oneYear);
                    var payCcyRate = ccyPay.get_Rate(oneYear);

                    var quantoDrift = insCcyRate - fxvol * undvol * correlation;
                    retVal = quantoDrift - payCcyRate;
                    
                    return retVal * 100;
                }
                catch
                {
                }

                return 0;

            }
            set
            {
                calculatedQuantoFee = value;
                NotifyPropertyChanged("CalculatedQuantoFee");
                NotifyPropertyChanged("Warning");
            }
        }

        public DateTime? NextObservationDate
        {
            get
            {
                DateTime? retVal = null;
                try
                {
                    DateTime dt = ObservationStart;
                    SR2COM.IEngine engine = new SR2COM.Engine();
                    SR2COM.IInstrument ins = engine.get_Instrument(Reference);

                    while (dt < Engine.Instance.Today)
                    {
                        dt = engine.Risque.get_RelativeToAbsoluteMaturity(Frequency, dt);
                    }

                    dt = ins.Calendar.get_MatchingBusinessDay(dt);
                    return dt;
                }
                catch
                {
                }
                return retVal;
            }
        }

        public bool BelongsTo(string workset)
        {
            bool isindexworkset = workset.ToLower().Contains("indices") || workset.ToLower().Contains("index");
            return isindexworkset;
        }

        public string Warning
        {
            get
            {
                SR2COM.IEngine engine = new SR2COM.Engine();
                SR2COM.IInstrument ins = engine.get_Instrument(Reference);
                string retVal = string.Empty;
                try
                {
                    SR2COM.IInstrument und = engine.get_Instrument(Underlying);
                    var nod = NextObservationDate;

                    if (DeactivateUntilNext)
                    {
                        if (ins.Calendar.AddNumberOfDays(nod.Value, -3) <= Engine.Instance.Today && nod.Value >= Engine.Instance.Today)
                        {
                            return retVal;
                        }
                        else
                            DeactivateUntilNext = false;
                    }
                    
                    if (nod == Engine.Instance.Today)
                    {
                        double quantoFee = CalculatedQuantoFee.Value;
                        DateTime dt = DateTime.Now;
                        
                        var quantoFees = QuantoFees.Where( x => x.Key != null).ToList();
                        if (quantoFees.Count(x => x.Key.Value.Date == DateTime.Today) > 0)
                        {
                            quantoFees.RemoveAll(x => x.Key.Value.Date == DateTime.Today);
                        }
                        
                        quantoFees.Add(new KeyValuePair<DateTime?, double?>(dt, quantoFee));

                        QuantoFees = quantoFees.OrderByDescending( x => x.Key ).ToList();

                        if (Math.Abs((UsedQuantoFee - CalculatedQuantoFee.Value)) > Threshold)
                            return String.Format("Quanto Fee threshold check failed - {0} | {1}",
                                ins.Reference,
                                    und.Reference);
                        else
                            return String.Format("Please adjust Quanto Fee – {0} – Calculated Fee {1:0.00}% vs Current Fee {2:0.00}%", 
                                ins.Reference, 
                                    CalculatedQuantoFee, 
                                        UsedQuantoFee);
                    }
                    else
                    if (ins.Calendar.AddNumberOfDays(nod.Value, -3) <= Engine.Instance.Today && nod.Value > Engine.Instance.Today)
                    {
                        if (und.VolatilityModificationTime < ins.Calendar.AddNumberOfDays(nod.Value, -3))
                            retVal = String.Format("Upcoming Quanto Fee check – {0} – Please update Volatility Surface of {1}",
                                ins.Reference, 
                                    und.Reference);
                    }
                }
                catch
                {
                }
                return retVal;
            }
        }

        double?[] quantoFee = new double?[4] {null, null, null, null};
        DateTime?[] quantoFeeDate = new DateTime?[4] { null, null, null, null };

        public List<KeyValuePair<DateTime?, double?>> QuantoFees
        {
            get
            {
                var retval = new List<KeyValuePair<DateTime?, double?>>();
                for( int i=0; i<4; i++)
                    retval.Add(new KeyValuePair<DateTime?,double?>(quantoFeeDate[i], quantoFee[i])) ;
                
                return retval;
            }
            set
            {
                int i = 0;
                
                for (i = 0; i < 4; i++)
                {
                    quantoFee[i] = null;
                    quantoFeeDate[i] = null;
                }
                
                i = 0;
                foreach (var item in value)
                {
                    if ( i < 4)
                    {
                        quantoFeeDate[i] = item.Key;
                        quantoFee[i] = item.Value;
                    }
                    i++;
                }
            }
        }
        
        public double? QuantoFee1
        {
            get
            {
                return quantoFee[0];
            }
            set
            {
                quantoFee[0] = value;
                NotifyPropertyChanged("QuantoFee1");
            }
        }

        public DateTime? QuantoFeeDate1
        {
            get
            {
                return quantoFeeDate[0];
            }
            set
            {
                quantoFeeDate[0] = value;
                NotifyPropertyChanged("QuantoFeeDate1");
            }
        }

        public double? QuantoFee2
        {
            get
            {
                return quantoFee[1];
            }
            set
            {
                quantoFee[1] = value;
                NotifyPropertyChanged("QuantoFee2");
            }
        }

        public DateTime? QuantoFeeDate2
        {
            get
            {
                return quantoFeeDate[1];
            }
            set
            {
                quantoFeeDate[1] = value;
                NotifyPropertyChanged("QuantoFeeDate2");
            }
        }

        public double? QuantoFee3
        {
            get
            {
                return quantoFee[2];
            }
            set
            {
                quantoFee[2] = value;
                NotifyPropertyChanged("QuantoFee3");
            }
        }

        public DateTime? QuantoFeeDate3
        {
            get
            {
                return quantoFeeDate[2];
            }
            set
            {
                quantoFeeDate[2] = value;
                NotifyPropertyChanged("QuantoFeeDate3");
            }
        }
        public double? QuantoFee4
        {
            get
            {
                return quantoFee[3];
            }
            set
            {
                quantoFee[3] = value;
                NotifyPropertyChanged("QuantoFee4");
            }
        }

        public DateTime? QuantoFeeDate4
        {
            get
            {
                return quantoFeeDate[3];
            }
            set
            {
                quantoFeeDate[3] = value;
                NotifyPropertyChanged("QuantoFeeDate4");
            }
        }

        bool Update(SR2COM.IInstrument ins, double fee)
        {

            var coms = ins.Compositions ;
            if (coms != null && coms.Count > 0)
            {
                foreach (SR2COM.IComposition com in coms)
                {
                    if (Update(com.Instrument, fee))
                        break;
                }
            }
            else
            {
                var fees = new List<KeyValuePair<DateTime, double>>();
                string info = String.Empty;

                foreach (SR2COM.IClause clause in ins.Clauses)
                {
                    if (clause != null && clause.TypeName == ManagementFeeName)
                    {
                        if (!String.IsNullOrEmpty(clause.Comment))
                            info = clause.Comment;

                        if (clause.EndDate >= Engine.Instance.Today || clause.EndDate <= zeroDate)
                            fees.Add(new KeyValuePair<DateTime, double>((clause.EndDate <= zeroDate ? DateTime.MaxValue : clause.EndDate), clause.Value));
                    }
                }

                if (fees.Any())
                {
                    var clauses = new List<SR2COM.IClause>();
                    foreach (SR2COM.IClause clause in ins.Clauses)
                    {
                        if (clause.TypeName != ManagementFeeName)
                            clauses.Add(clause);
                    }

                    var clone = ins.Clone;
                    clone.RemoveAllClauses();

                    foreach (SR2COM.IClause clause in clauses)
                        clone.AddClause(clause.TypeName, clause.StartDate, clause.EndDate, clause.PaymentDate, clause.Value, clause.Min, clause.Max, clause.Comment);

                    foreach (var entry in fees.Where(x => x.Key > Engine.Instance.Today))
                        clone.AddClause(ManagementFeeName, zeroDate, Engine.Instance.Today, zeroDate, entry.Value, 0, 0, info);

                    clone.AddClause(ManagementFeeName, zeroDate, DateTime.MaxValue.Date, zeroDate, fee, 0, 0, info);

                    clone.Save();

                    NotifyPropertyChanged("UsedQuantoFee");
                    NotifyPropertyChanged("ManagementFee");

                    return true;
                }

            }

            return false;
        }
        
        public void Update()
        {
            try
            {
                SR2COM.IEngine engine = new SR2COM.Engine();
                SR2COM.IInstrument ins = engine.get_Instrument(Reference);

                Update(ins, CalculatedQuantoFee.Value + AdditionalManagementFee);

            }
            catch (Exception ex)
            {
                Engine.Instance.ErrorException("error while update management fee", ex);
                throw ex;
            }
        }
    }
}
