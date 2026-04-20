using System;

namespace Puma.MDE.Data
{

    public class CAExecutionLog
    {
        public bool Equals(CAExecutionLog other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return (other.UnderlyingSicovam == UnderlyingSicovam && 
                    other.CorporateActionId == CorporateActionId &&
                    other.Sicovam == Sicovam);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(CAExecutionLog)) return false;
            return Equals((CAExecutionLog)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (UnderlyingSicovam * 397) ^ CorporateActionId ^ Sicovam;
            }
        }

        public int CorporateActionId
        {
            get;
            set;
        }
        public int UnderlyingSicovam
        {
            get;
            set;
        }

        public int LogType
        {
            get;
            set;
        }

        public int LogCategory
        {
            get;
            set;
        }

        public int UserId
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        }

        public DateTime LogTime
        {
            get;
            set;
        }

        public int Sicovam
        {
            get;
            set;
        }
    }





    public class CAExecutionCheck
    {
        public bool Equals(CAExecutionCheck other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Sicovam == Sicovam && other.CorporateActionId == CorporateActionId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(CAExecutionCheck)) return false;
            return Equals((CAExecutionCheck)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Sicovam * 397) ^ CorporateActionId;
            }
        }

        public int CorporateActionId     
        {
            get;
            set;
        }
        public int Sicovam               
        {
            get;
            set;
        }
        public int? ActionStatus         
        {
            get;
            set;
        }
        public int? LoUpdater            
        {
            get;
            set;
        }
        public int? PoUpdater            
        {
            get;
            set;
        }
        public int? Volatility           
        {
            get;
            set;
        }
        public int? Dividend             
        {
            get;
            set;
        }
        public int? Ajustement           
        {
            get;
            set;
        }
        public int? SpotUpdater          
        {
            get;
            set;
        }
        public int? BasketReplacement    
        {
            get;
            set;
        }

        //enum EExecutionStatusType: {esNull=0, esNotApplicable=1, esReadyForSimulation=2, esReadyForExecution=3, esExecuted=4, esError=5};
        internal caExecEnum ToCAExecEnum(int? value)
        {
            if      (this == null)                              { return caExecEnum.NotAppicable; }
            else if (value.HasValue == false|| value.Value < 4) { return caExecEnum.NotAppicable; }
            else if (value.Value == 4)                          { return caExecEnum.OK; }
            else if (value.Value == 5)                          { return caExecEnum.Error; }
            
            return caExecEnum.NotAppicable;
        }
    }



}
