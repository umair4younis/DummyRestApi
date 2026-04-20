using Puma.MDE.Common;
using Puma.MDE.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if SOPHIS_7
using NHibernate.Util;
#endif

namespace Puma.MDE.Data
{
    [Serializable]
    public class QuixComposition : Entity
    {
        public const string CorporateActionProcessing = "CA Processed";
        public const string CorporateActionErrorProcessing = "CA Error Processed";
        public QuixComposition()
        {
            QuixComponents = new List<QuixComponent>();
            // initialize validFrom date based on ReweightStart date plus reweight period
            _validFrom = WorkingDays.AddWorkingDays(ReweightStart, ReweightPeriod);
        }

        public QuixComposition(QuixComposition composition)
        {
            _validFrom = composition.ValidFrom;
            _reweightStart = composition.ReweightStart;
            _reweightPeriod = composition.ReweightPeriod;
            _comments = composition.Comments;
            _reweightFailReason = composition.ReweightFailReason;
            _oldAmended = composition.OldAmended;
            _feeStartDate = composition.FeeStartDate;
            _rebalancingFee = composition.RebalancingFee;
            _indexDividendRate = composition.IndexDividendRate;

            Underlying = composition.Underlying;
            QuixComponents = composition.QuixComponents;
        }

        public QuixComposition(Underlying underlying)
        {
            Underlying = underlying;
            QuixComponents = new List<QuixComponent>();
            // initialize validFrom date based on ReweightStart date plus reweight period
            _validFrom = WorkingDays.AddWorkingDays(ReweightStart, ReweightPeriod);
        }

        public QuixComposition(Underlying underlying, DateTime reweightStart, int reweightPeriod, DateTime validFrom)
        {
            Underlying = underlying;
            QuixComponents = new List<QuixComponent>();

            _reweightStart = reweightStart;
            _reweightPeriod = reweightPeriod;
            _validFrom = validFrom;
        }

        public void CopyFrom(QuixComposition composition)
        {
            //_validFrom = composition.ValidFrom;
            ReweightStart = composition.ReweightStart;
            ReweightPeriod = composition.ReweightPeriod;
            Comments = composition.Comments;
            ReweightFailReason = composition.ReweightFailReason;
            OldAmended = composition.OldAmended;
            FeeStartDate = composition.FeeStartDate;
            RebalancingFee = composition.RebalancingFee;
            IndexDividendRate = composition.IndexDividendRate;

            Underlying = composition.Underlying;
            QuixComponents = composition.QuixComponents;
        }

        public Underlying Underlying { get; set; }

        public IList<QuixComponent> QuixComponents { get; set; }

        public IList<QuixComponent> AvailableQuixComponents
        {
            get { return QuixComponents.Where(component => component.IsSetToDelete == false).ToList(); }
        }

        public DateTime CalculationDay = DateTime.Today;

        private DateTime _validFrom = DateTime.Now;
        public DateTime ValidFrom
        {
            get
            {
                return _validFrom;
            }
            set
            {
                NotifyPropertyChangedDirty(ref _validFrom, value, () => ValidFrom);
            }
        }

        private DateTime _reweightStart = DateTime.Now;
        public DateTime ReweightStart
        {
            get { return _reweightStart; }
            set
            {
                NotifyPropertyChangedDirty(ref _reweightStart, value, () => ReweightStart);
                ValidFrom = WorkingDays.AddWorkingDays(ReweightStart, ReweightPeriod);
                if (Underlying != null && Underlying.QuixReweight != null && Underlying.QuixReweight.QuixReweightModel != null && (Underlying.QuixReweight.QuixReweightModel.IsStandardFee || Underlying.QuixReweight.QuixReweightModel.IsManualFee))
                {
                    var today = DateTime.Today;
                    if (_reweightStart.CompareTo(today) >= 0)
                    {
                        FeeStartDate = _reweightStart.Date;
                    }
                }
            }
        }

        private int _reweightPeriod = 1;
        public int ReweightPeriod
        {
            get { return _reweightPeriod; }
            set
            {
                NotifyPropertyChangedDirty(ref _reweightPeriod, value, () => ReweightPeriod);
                ValidFrom = WorkingDays.AddWorkingDays(ReweightStart, ReweightPeriod);
            }
        }

        private bool _oldAmended;
        public bool OldAmended
        {
            get { return _oldAmended; }
            set
            {
                NotifyPropertyChangedDirty(ref _oldAmended, value, () => OldAmended);
            }
        }

        private string _comments;
        public string Comments
        {
            get { return _comments; }
            set
            {
                NotifyPropertyChangedDirty(ref _comments, value, () => Comments);
            }
        }

        private string _reweightFailReason;
        public string ReweightFailReason
        {
            get { return _reweightFailReason; }
            set
            {
                NotifyPropertyChangedDirty(ref _reweightFailReason, value, () => ReweightFailReason);
            }
        }

        private string _validatedUser;
        public string ValidatedUser
        {
            get { return _validatedUser; }
            set
            {
                NotifyPropertyChangedDirty(ref _validatedUser, value, () => ValidatedUser);
            }
        }

        private DateTime? _validatedTime;
        public DateTime? ValidatedTime
        {
            get { return _validatedTime; }
            set
            {
                NotifyPropertyChangedDirty(ref _validatedTime, value, () => ValidatedTime);
            }
        }

        private DateTime? _feeStartDate;
        public DateTime? FeeStartDate
        {
            get { return _feeStartDate; }
            set
            {
                    NotifyPropertyChangedDirty(ref _feeStartDate, value, () => FeeStartDate);
            }
        }


        private bool _publishingError;
        public bool PublishingError
        {
            get { return _publishingError; }
            set
            {
                NotifyPropertyChangedDirty(ref _publishingError, value, () => PublishingError);
            }
        }

        private double _rebalancingFee = 0;
        public double RebalancingFee
        {
            get { return _rebalancingFee; }
            set
            {
                NotifyPropertyChangedDirty(ref _rebalancingFee, value, () => RebalancingFee);
            }
        }

        private double _indexDividendRate = 0;
        public double IndexDividendRate
        {
            get { return _indexDividendRate; }
            set
            {
                NotifyPropertyChangedDirty(ref _indexDividendRate, value, () => IndexDividendRate);
            }
        }


        public bool IsPastComposition
        {
            get { return ValidFrom.Date < DateTime.Today; }
        }

        public string BalanceRatio
        {
            get {
                int count = ComponentCount - AvailableQuixComponents.Count(component => component.IsFeeComponent);
                return ComponentCount > 0 ?  string.Format("Balance 1/{0}", count) : "Balance 1/N"; }
        }

        public int ComponentCount
        {
            get { return AvailableQuixComponents.Count; }
        }

        public bool HasComponents
        {
            get { return ComponentCount > 0; }
        }

        public int ComponentWithoutWeightCount
        {
            get { return AvailableQuixComponents.Count(comp => comp.Weight.HasValue == false); }
        }

        public double ComponentWeightsSum
        {
            get { return AvailableQuixComponents.Where(comp => comp.Weight.HasValue).Sum(comp => comp.Weight).Value; }
        }

        public double IndexPrice
        {
            get
            {
                var price = AvailableQuixComponents.Sum(component => (component.Spi ?? 0) * ((component.Price ?? 0.0) / (component.FxRate ?? 1)));

                return Math.Round(price, Underlying.QuixIndexPrecision);
            }
        }

        public bool CanBeReweighted
        {
            get
            {
                var today = DateTime.Today;
                return (ReweightStart.Date < today && today <= ValidFrom.Date);
            }
        }

        public QuixComponent FeeComponent
        {
            get
            {
                return AvailableQuixComponents.SingleOrDefault(hvb => hvb.IsFeeComponent == true);
            }
        }

        //public bool CanBeUpdated
        //{
        //    get
        //    {
        //        // Regardless of New(Id == 0) or DbSaved(Id != 0) we want to get the current selected composition updated
        //        if(CalculationDay.Date == ValidFrom.Date)
        //        {
        //            return true;
        //        }

        //        // This is an DbSaved(Id != 0) and Intermediate(CalculationDay < ValidFrom) so we update the saved intermediate one but not current selected one
        //        if (IsPersisted && CalculationDay.Date < ValidFrom.Date)
        //        {
        //            return true;
        //        }

        //        // If it is a New(Id == 0) and Intermediate(CalculationDay < ValidFrom) composition we should create a new object
        //        if(IsTransient && CalculationDay.Date < ValidFrom.Date)
        //        {
        //            return false;
        //        }

        //        return true;
        //    }
        //}

        public override int? Identity()
        {
            int result = _validFrom.GetHashCode();
            result = (result * 397) ^ _reweightStart.GetHashCode();
            result = (result * 397) ^ _reweightPeriod;
            return result;
        }

        public override string ToString()
        {
            return string.Format("Index '{0}', ValidFrom = '{1}', ReweightStart= '{2}', Period = '{3}'",
                                 Underlying.RIC, ValidFrom.ToString("yyyyMMdd"), ReweightStart.ToString("yyyyMMdd"),
                                 ReweightPeriod);
        }

        public bool AddQuixComponent(QuixComponent addedQuixComponent)
        {
            if (addedQuixComponent == null || QuixComponents == null)
                return false;

            var anyWithTheSameUnderlying = AvailableQuixComponents.Any(comp => comp.Underlying.Reference == addedQuixComponent.Reference);
            if (anyWithTheSameUnderlying)
                return false;

            var newComponent = new QuixComponent(addedQuixComponent);
            QuixComponents.Add(newComponent);

            SetToDirty();

            return true;
        }

        public void RemoveQuixComponent(QuixComponent quixComponent)
        {
            if (QuixComponents.Contains(quixComponent))
            {
                QuixComponents.Remove(quixComponent);
            }
        }

        public void RemoveAnyNewQuixComponents()
        {
            // First remove the ones that are not persisted and just pasted from excel or added through add dialog box
            // and not saved but now they are decided that they are not needed
            var temList = QuixComponents.ToList();
            temList.RemoveAll(component => true);
            QuixComponents = temList;
        }

        public void RemoveAnyExistingQuixComponents()
        {
        }


        public void RemoveAllQuixComponents()
        {
            // First remove the ones that are not persisted and just pasted from excel or added through add dialog box
            // and not saved but now they are decided that they are not needed
            RemoveAnyNewQuixComponents();

            // Now make sure that persisted ones that exist in db are marked to be deleted
            // and when deleted from database and then they are removed completely
            RemoveAnyExistingQuixComponents();
        }

        public void BalanceWeightsRatio()
        {
            var count = ComponentCount - AvailableQuixComponents.Count(component => component.IsFeeComponent);
            if (count <= 0) return;
        }

        public void AddComments(string addedComment)
        {
            var today = string.Format("({0})", DateTime.Today.Date.ToString("dd.MM.yyyy"));
            var commentsBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(Comments))
            {
                var indexOfToday = Comments.IndexOf(today, System.StringComparison.Ordinal);
                if (indexOfToday != -1)
                {
                    var fromTodayButEarlier = Comments.Substring(indexOfToday);
                    commentsBuilder.AppendFormat(fromTodayButEarlier);
                }
            }

            commentsBuilder.AppendLine(addedComment);
            IsNotificationEnabled = false;
            Comments = commentsBuilder.ToString();
            IsNotificationEnabled = true;
        }

        public void AddReweightFailReason(string addedFailedReason)
        {
            var reweightFailReason = string.Format("({0}) {1}", DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss"), addedFailedReason);
            IsNotificationEnabled = false;

            if (reweightFailReason.Length > 500)
            {
                ReweightFailReason = reweightFailReason.Substring(0, 500);
            }
            else
            {
                ReweightFailReason = reweightFailReason;
            }

            IsNotificationEnabled = true;
        }


        public void AdjustSpisBasedOnIndexPrice(double indexPrice, bool givenPriceAndFx, bool givenSpi, bool givenWeight)
        {
            var comment = string.Format("({0}) Composition based on Manual entered price = {1}", DateTime.Today.Date.ToString("dd.MM.yyyy"), indexPrice);
            AddComments(comment);
        }

        public double AdjustIndexPriceBasedOlderComposition(QuixComposition olderComposition)
        {
            var index = olderComposition.Underlying;
            var reweightModelType = index.GetQuixReweightModelProcessType();
            if (reweightModelType == null)
                return 0.0;

            var priceDay = ReweightStart.Date.AddHours(22);
            var indexPrice = 0.0;

            return indexPrice;
        }

        public static QuixComposition CreateNewComposition(QuixComposition quixComposition)
        {
            if (quixComposition == null)
                return null;

            var newComposition = new QuixComposition();

            newComposition.Underlying = quixComposition.Underlying;
            newComposition.ValidFrom = quixComposition.ValidFrom;
            newComposition.ReweightPeriod = quixComposition.ReweightPeriod;
            newComposition.ReweightStart = quixComposition.ReweightStart;
            newComposition.Comments = quixComposition.Comments;
            newComposition.QuixComponents = new List<QuixComponent>();
            newComposition.ValidatedUser = quixComposition.ValidatedUser;
            newComposition.ValidatedTime = quixComposition.ValidatedTime;
            newComposition.FeeStartDate = quixComposition.FeeStartDate;
            newComposition.RebalancingFee = quixComposition.RebalancingFee;
            newComposition.IndexDividendRate = quixComposition.IndexDividendRate;

            // since composition is new all components also must be
            foreach (var component in quixComposition.QuixComponents)
            {
                var clonedComp = new QuixComponent(component.Underlying, newComposition);

                clonedComp.Weight = component.Weight;
                clonedComp.Spi = component.Spi;
                clonedComp.Price = component.Price;
                clonedComp.PriceManual = component.PriceManual;
                clonedComp.Currency = component.Currency;
                clonedComp.FxRate = component.FxRate;
                clonedComp.SpotLossReflevel = component.SpotLossReflevel;

                newComposition.QuixComponents.Add(clonedComp);
            }

            return newComposition;
        }

        public static QuixComposition CreateNewComposition(List<QuixComponentAudit> components, QuixCompositionAudit composition)
        {
            if (components == null)
                return null;

            var newComposition = new QuixComposition();

            newComposition.Underlying = composition.Underlying;
            newComposition.ValidFrom = composition.ValidFrom;
            newComposition.ReweightPeriod = composition.ReweightPeriod;
            newComposition.ReweightStart = composition.ReweightStart;
            newComposition.Comments = composition.Comments;
            newComposition.QuixComponents = new List<QuixComponent>();
            newComposition.ValidatedUser = "";// to be added in a final version composition.ValidatedUser;
            newComposition.ValidatedTime = DateTime.Now;// to be added in a final version  composition.ValidatedTime;
            newComposition.FeeStartDate = DateTime.Now;// to be added in a final versioncomposition.FeeStartDate;
            newComposition.RebalancingFee = 0;
            newComposition.IndexDividendRate = 0;

            // since composition is new all components also must be
            foreach (var component in components)
            {
                var clonedComp = new QuixComponent(component.Underlying, newComposition);

                clonedComp.Weight = component.Weight;
                clonedComp.Spi = component.Spi;
                clonedComp.Price = component.Price;
                clonedComp.PriceManual = component.PriceManual;
                clonedComp.Currency = component.Currency;
                clonedComp.FxRate = component.FxRate;
                clonedComp.SpotLossReflevel = component.SpotLossReflevel;

                newComposition.QuixComponents.Add(clonedComp);
            }

            return newComposition;
        }


    }
    public class QuixCompositionAudit : Audit, IEquatable<QuixCompositionAudit>
    {
        public QuixCompositionAudit()
        {
        }

        public Underlying Underlying { get; set; }

        private DateTime _validFrom = DateTime.Now;
        public DateTime ValidFrom
        {
            get { return _validFrom; }
            set
            {
                NotifyPropertyChangedDirty(ref _validFrom, value, () => ValidFrom);
                
            }
        }

        private DateTime _reweightStart = DateTime.Now;
        public DateTime ReweightStart
        {
            get { return _reweightStart; }
            set
            {
                NotifyPropertyChangedDirty(ref _reweightStart, value, () => ReweightStart);
                
            }
        }

        private int _reweightPeriod = 1;
        public int ReweightPeriod
        {
            get { return _reweightPeriod; }
            set
            {
                NotifyPropertyChangedDirty(ref _reweightPeriod, value, () => ReweightPeriod);
                
            }
        }

        private bool _oldAmended;
        public bool OldAmended
        {
            get { return _oldAmended; }
            set
            {
                NotifyPropertyChangedDirty(ref _oldAmended, value, () => OldAmended);
            }
        }

        private string _comments;
        public string Comments
        {
            get { return _comments; }
            set
            {
                NotifyPropertyChangedDirty(ref _comments, value, () => Comments);
            }
        }

        private string _reweightFailReason;
        public string ReweightFailReason
        {
            get { return _reweightFailReason; }
            set
            {
                NotifyPropertyChangedDirty(ref _reweightFailReason, value, () => ReweightFailReason);
            }
        }

        public override string EntityName
        {
            get { return "QuixComposition"; }
        }

       
        public bool Equals(QuixCompositionAudit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._validFrom.Equals(_validFrom) && 
                other._reweightStart.Equals(_reweightStart) && 
                other._reweightPeriod == _reweightPeriod && 
                Equals(other._comments, _comments) &&
                Equals(other._reweightFailReason, _reweightFailReason) &&
                Equals(other.JournalTimeStamp, JournalTimeStamp) &&
                Equals(other.JournalType, JournalType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (QuixCompositionAudit)) return false;
            return Equals((QuixCompositionAudit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _validFrom.GetHashCode();
                result = (result*397) ^ _reweightStart.GetHashCode();
                result = (result*397) ^ _reweightPeriod;
                result = (result*397) ^ (_comments != null ? _comments.GetHashCode() : 0);
                result = (result*397) ^ (_reweightFailReason != null ? _reweightFailReason.GetHashCode() : 0);
                result = (result*397) ^ (JournalType != null ? JournalType.GetHashCode() : 0);
                result = (result*397) ^ JournalTimeStamp.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(QuixCompositionAudit left, QuixCompositionAudit right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(QuixCompositionAudit left, QuixCompositionAudit right)
        {
            return !Equals(left, right);
        }
    }
}
