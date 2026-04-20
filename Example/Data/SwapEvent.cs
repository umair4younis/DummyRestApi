using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Puma.MDE.Common;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapEvent : Entity
    {
        public SwapEvent() { this.Id = 1; }
        public SwapEvent(int id) { this.Id = id; }
        public String DbName { get; set; }
        public int EventType { get; set; }
        public TypeManastEvent EventTypeEnum
        {
            get => (TypeManastEvent)EventType;
            set
            {
                EventType = (int)value;
                NotifyPropertyChanged(nameof(EventTypeEnum));
                NotifyPropertyChanged(nameof(EventType));
            }
        }
        public int EventAction { get; set; }
        public DateTime ExecutionDate { get; set; }
        public DateTime? Executed { get; set; }
        public bool IsExecutedNull()
        {
            return !Executed.HasValue;
        }

        public void SetExecutedNull()
        {
            Executed = null;
            NotifyPropertyChanged(nameof(Executed));
        }

        public DateTime? ExDate { get; set; }
        public bool IsExDateNull()
        {
            return !ExDate.HasValue;
        }
        public DateTime xExDate
        {
            get
            {
                return (IsExDateNull()) ? DateTime.MinValue : ExDate.Value;
            }
        }

        public String IsManual { get; set; }

        public int? PortfolioRowByPortfolioSourceId { get; set; }

        [JsonIgnore]
        public SwapAccountPortfolio PortfolioRowByPortfolioSource
        {
            get
            {
                if (PortfolioRowByPortfolioSourceId.HasValue)
                    return new SwapAccountPortfolio();
                else return null;
            }
            set
            {
                if (value == null)
                {
                    PortfolioRowByPortfolioSourceId = null;
                }
                else
                {
                    PortfolioRowByPortfolioSourceId = value.Id;
                }
                NotifyPropertyChanged(() => PortfolioRowByPortfolioSourceId);
                NotifyPropertyChanged(() => PortfolioRowByPortfolioSource);
            }
        }

        public int? PortfolioRowByPortfolioTargetId { get; set; }

        [JsonIgnore]
        public SwapAccountPortfolio PortfolioRowByPortfolioTarget
        {
            get
            {
                if (PortfolioRowByPortfolioTargetId.HasValue)
                    return new SwapAccountPortfolio();
                else return null;
            }
            set
            {
                if (value == null)
                {
                    PortfolioRowByPortfolioTargetId = null;
                }
                else
                {
                    PortfolioRowByPortfolioTargetId = value.Id;
                }
                NotifyPropertyChanged(() => PortfolioRowByPortfolioTargetId);
                NotifyPropertyChanged(() => PortfolioRowByPortfolioTarget);
            }
        }
        public double Nominal { get; set; }
        public String Currency { get; set; }
        public double FXRate { get; set; }
        public String Description { get; set; }
        public double? xBasis { get; set; }
        public double Basis
        {
            get
            {
                if (IsBasisNull()) return 0;
                else return xBasis.Value;
            }
            set
            {
                xBasis = value;
                NotifyPropertyChanged(() => xBasis);
                NotifyPropertyChanged(() => Basis);
            }
        }
        public bool IsBasisNull() { return !xBasis.HasValue; }

        public double? Value1 { get; set; }
        public double xValue1
        {
            get
            {
                return Value1.HasValue ? Value1.Value : 0;
            }
        }
        public DateTime? PayDate { get; set; }

        public bool IsPayDateNull()
        {
            return !PayDate.HasValue;
        }

        public DateTime xPayDate
        {
            get
            {
                return (IsPayDateNull()) ? DateTime.MinValue : PayDate.Value;
            }
        }

        public double? Factor { get; set; }
        public double xFactor
        {
            get
            {
                return Factor.HasValue ? Factor.Value : 0;
            }
        }
        public SwapUser User { get; set; }

        public String UserLoginOrNull
        {
            get
            {
                return User == null ? null : User.Login;
            }
        }

        public int RowVersion { get; set; }

        public SwapAccountHistory AccountHistoryRow { get; set; }
        public double? Ratio { get; set; }

        /// <summary>
        /// Avoids an InvalidCastException in the event grid if the Executed value is null
        /// </summary>
        /// <value>The x executed.</value>
        public String xExecuted
        {
            get
            {
                if (IsExecutedNull())
                    return "no";
                else
                    return Executed.Value.ToString("dd/MM/yy hh:mm");
            }
        }

        /// <summary>
        /// Gets or sets the DateExecuted value with null value handling.
        /// Avoids the occurrence of exceptions when the DateExecuted field value is called from the grid.
        /// </summary>
        /// <value>The date executed or null.</value>
        public Object ExecutedOrNull
        {
            get
            {
                return (IsExecutedNull()) ? null : (object)Executed;
            }
            set
            {
                if (value != null)
                    Executed = (DateTime)value;
                else
                    SetExecutedNull();
            }
        }

    }
}
