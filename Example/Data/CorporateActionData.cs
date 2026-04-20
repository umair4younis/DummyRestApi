using System;
using System.Collections.Generic;
using System.Linq;

namespace Puma.MDE.Data
{
    public class CorporateActionData
    {
        public CorporateActionData()
        {
            IsDirty = false;
            IsValid = true;
        }

        public bool Equals(CorporateActionData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.SophisInstrumentId == SophisInstrumentId && other.CorporateActionId == CorporateActionId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(CorporateActionData)) return false;
            return Equals((CorporateActionData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SophisInstrumentId * 397) ^ CorporateActionId;
            }
        }

        public int StatusId
        {
            get;
            set;
        }

        public bool IsDirty
        {
            get;
            set;
        }
        public bool IsValid
        {
            get;
            set;
        }
        public bool B2BMayEdit
        {
            get;
            set;
        }
        public bool B2BIsReadOnly
        {
            get;
            set;
        }

        public static int Weekdays(DateTime dtmStart, DateTime dtmEnd)
        {            
            int dowStart = ((int)dtmStart.DayOfWeek == 0 ? 7 : (int)dtmStart.DayOfWeek);
            int dowEnd = ((int)dtmEnd.DayOfWeek == 0 ? 7 : (int)dtmEnd.DayOfWeek);
            TimeSpan tSpan = dtmEnd - dtmStart;
            if (dowStart <= dowEnd)
            {
                return (((tSpan.Days / 7) * 5) + Math.Max((Math.Min((dowEnd + 1), 6) - dowStart), 0));
            }
            return (((tSpan.Days / 7) * 5) + Math.Min((dowEnd + 6) - Math.Min(dowStart, 6), 5));
        }

        public int UnderlyingSicovam
        {
            get;
            set;
        }
        public int BusinessDaysToExDay
        {
            get;
            set;
        }
        public int CorporateActionId
        {
            get;
            set;
        }
        public string NewCorporateActionStatus
        {
            get;
            set;
        }
        public string CorporateActionType
        {
            get;
            set;
        }
        private int _corporateActionTypeId;
        public int CorporateActionTypeId
        {
            get
            {
                return _corporateActionTypeId;
            }
            set
            {
                _corporateActionTypeId = value;
                if (_corporateActionTypeId == 40)
                {
                    CorporateActionType = "HVB R-Factor";
                }
                else
                    CorporateActionType = "Unknown";
            }
        }
        public int SophisInstrumentId
        {
            get;
            set;
        }
        public String SophisReference
        {
            get;
            set;
        }
        public String Comment
        {
            get;
            set;
        }        
        public string InstrumentName
        {
            get;
            set;
        }
        public int NumberOfUnderlyingPositionsMIB
        {
            get;
            set;
        }
        public int NumberOfDerivativePositionsMIB
        {
            get;
            set;
        }
        public int NumberOfUnderlyingPositionsUnderEntryPoint
        {
            get;
            set;
        }
        public int NumberOfDerivativePositionsUnderEntryPoint
        {
            get;
            set;
        }
        private DateTime _exDate;
        public DateTime ExDate
        {
            get
            {
                return _exDate;
            }
            set
            {
                _exDate = value;
                int noDays = 0;
                TimeSpan ts = DateTime.Today - _exDate;
                if (ts.Days > 0)
                {
                    noDays = Weekdays(_exDate, DateTime.Today) - 1;
                    noDays *= -1;
                }
                else
                {
                    noDays = Weekdays(DateTime.Today, _exDate) - 1;
                }
                BusinessDaysToExDay = noDays;
            }
        }
        public Decimal RFactor
        {
            get;
            set;
        }
        public bool UpdateMarketData
        {
            get;
            set;
        }
        public bool PublishCorporateAction
        {
            get;
            set;
        }
        public bool ReplaceUnderlyingByBasket
        {
            get;
            set;
        }
        public int ReplacementBasketSophisId
        {
            get;
            set;
        }
        public bool B2BReplacement
        {
            get;
            set;
        }
        public int Basket2BasketNewSophisId
        {
            get;
            set;
        }
        public int Basket2BasketOldSophisId
        {
            get;
            set;
        }
        public bool LocalExecution
        {
            get;
            set;
        }

        public string CurrentSimulStatus
        {
            get;
            set;
        }

        public string ShortDescription
        {
            get
            {
                return this.CorporateActionId + " " + this.ExDate.ToShortDateString() + " " + this.SophisReference;
            }
        }

        public int CurrentSimulStatusId
         {
             get;
             set;
         }

        public string CurrentLegalType
        {
            get;
            set;
        }

        public int CurrentLegalTypeId
         {
             get;
             set;
         }

        public bool AllowLocalExecution
         {
             get
             {      //request pof trading: allow local execution for past CAs which have been not executed already
                 if (ExDate.Date <= DateTime.Today.Date && CurrentSimulStatusId != 8 && CurrentSimulStatusId !=10)
                     return true;

                 return false;
             }
         }
    }

    public enum caExecEnum { OK = 0, Error = 1, NotAppicable = 2 };
   

    public class CAExecutionChecksData : CorporateActionData
    {
        public CAExecutionChecksData(CorporateActionData ca, CAExecutionCheck execCheck, IList<CAExecutionLog> orcExecutionLogs)
        {
            Set(ca);
            Set(execCheck);
            Set(orcExecutionLogs);
        }

        private void Set(IList<CAExecutionLog> orcExecLogs)
        {
            if (orcExecLogs == null || orcExecLogs.Count == 0)
            {
                this.OrcAdjustedDivStatus = caExecEnum.NotAppicable;
                this.OrcAdjustedVolStatus = caExecEnum.NotAppicable;
                return;
            }
            //please look at .\mainline\SophisExtensions\PuMa.MDE\classes\PumaMDE.Auto.CorporateActionAdjustment\Program.cs 
            //in static method WriteDbLogging
            var orcDivLog = orcExecLogs.Where(i => i.LogCategory == 7).OrderByDescending(i => i.LogTime).FirstOrDefault();
            var orcVolLog = orcExecLogs.Where(i => i.LogCategory == 6).OrderByDescending(i => i.LogTime).FirstOrDefault();

            this.OrcAdjustedDivStatus = orcDivLog.ToCAExecEnum();
            this.OrcAdjustedVolStatus = orcVolLog.ToCAExecEnum();
        }

        private void Set(CAExecutionCheck check)
        {
            if (check == null)
            {
                this.VolAdjustStatus    = caExecEnum.NotAppicable;
                this.DivAdjustStatus    = caExecEnum.NotAppicable;
                this.SpotAdjustStatus   = caExecEnum.NotAppicable;
                this.SophisAjustStatus  = caExecEnum.NotAppicable;
                return;
            }
            this.VolAdjustStatus    = check.ToCAExecEnum(check.Volatility);
            this.DivAdjustStatus    = check.ToCAExecEnum(check.Dividend);
            this.SpotAdjustStatus   = check.ToCAExecEnum(check.SpotUpdater);
            this.SophisAjustStatus  = check.ToCAExecEnum(check.Ajustement);
        }

        private void Set(CorporateActionData caData)
        {
            this.BusinessDaysToExDay    = caData.BusinessDaysToExDay;
            this.CorporateActionId      = caData.CorporateActionId;
            this.CorporateActionType    = caData.CorporateActionType;
            this.SophisReference        = caData.SophisReference;
            this.SophisInstrumentId     = caData.SophisInstrumentId;
            this.InstrumentName         = caData.InstrumentName;
            this.Comment                = caData.Comment;
            this.ExDate                 = caData.ExDate;
            this.RFactor                = caData.RFactor;
        }

        public virtual caExecEnum DivAdjustStatus       { get; private set; }
        public virtual caExecEnum VolAdjustStatus       { get; private set; }
        public virtual caExecEnum SpotAdjustStatus      { get; private set; }
        public virtual caExecEnum SophisAjustStatus     { get; private set; }
        public virtual caExecEnum OrcAdjustedDivStatus  { get; private set; }
        public virtual caExecEnum OrcAdjustedVolStatus  { get; private set; }
    }


    static class CAEnumUtils
    {
        public static caExecEnum ToCAExecEnum(this CAExecutionLog value)
        {
            if (value == null)           { return caExecEnum.NotAppicable; }
            else if (value.LogType == 1) { return caExecEnum.OK; }
            else if (value.LogType == 3) { return caExecEnum.Error; }

            return caExecEnum.NotAppicable;
        }
    } 

}
