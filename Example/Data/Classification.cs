using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;


namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("DDAFCA98-2F02-482d-905D-F1257E303CB2")]
    public class Classifications : IEnumerable
    {
        IList<Classification> collection;
        [ComVisible(false)]
        public IList<Classification> Collection
        {
            get
            {
                return collection;
            }
        }
        public Classifications(IList<Classification> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(Classification item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public Classification this[int index]
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
        public override string ToString()
        {
            var commaSeparated = new StringBuilder();
            foreach(var item in collection)
            {
                commaSeparated.AppendFormat("{0},", item);
            }
            
            var all = commaSeparated.ToString();
            var returnAll =  (all.Length > 0 ) ? all.Substring(0, all.Length -1) : all;

            return returnAll;
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("50203490-434F-4918-983E-FE5AFC971538")]
    [ComVisible(true)]

    public class Classification
    {
        
       public int Id {get;set;}
       public String Name {get;set;}


       public Classifications Children
       {
           get
           {
               Classifications retval = new Classifications(new List<Classification>());
               foreach (ClassificationHierarchie h in Engine.Instance.Factory.GetChildClassifications(this))
               {
                   retval.Add(h.Child);
               }
               return retval;
           }
       }
       public Classifications Parents
       {
           get
           {
               Classifications retval = new Classifications(new List<Classification>());
               foreach (ClassificationHierarchie h in Engine.Instance.Factory.GetParentClassifications(this))
               {
                   retval.Add(h.Parent);
               }
               return retval;
           }
       }

       public QuoteFilterRule GetQuoteFilterRule(string typename)
       {
           if (typename.Length == 0)
               typename = "Standard";

           QuoteFilterType type = Engine.Instance.Factory.GetFilterType(typename);
           QuoteFilterRule retval = Engine.Instance.Factory.GetFilterRule(this, type);
           if (retval == null)
           {
               foreach (Classification parent in Parents)
               {
                   retval = parent.GetQuoteFilterRule(typename);
                   if (retval != null)
                       break;
               }
           }

           return retval;
       }

       public SettingsSetRule GetSettingsSetRule(string typename)
       {
           if (typename.Length == 0)
               typename = "Standard";

           SettingsSetType type = Engine.Instance.Factory.GetSettingsSetType(typename);
           SettingsSetRule retval = Engine.Instance.Factory.GetSettingsSetRule(this, type);
           if (retval == null)
           {
               foreach (Classification parent in Parents)
               {
                   retval = parent.GetSettingsSetRule(typename);
                   if (retval != null)
                       break;
               }
           }

           return retval;
       }

       public MaturitySchedule GetMaturitySchedule()
       {
           MaturitySchedule retval = Engine.Instance.Factory.GetMaturitySchedule(this);
           if (retval == null)
           {
               foreach (Classification parent in Parents)
               {
                   retval = parent.GetMaturitySchedule();
                   if (retval != null)
                       break;
               }
           }

           return retval;
       }

       [ComVisible(false)]
       public List<CommodityVolComponent> GetCommodityVolComponents() //VDP
       {
           List<CommodityVolComponent> componentTypes = new List<CommodityVolComponent>(); 
                      
           return (componentTypes == null) ? new List<CommodityVolComponent>() : componentTypes.ToList();
       }


       public CommodityVolParameter GetCommodityVolParametersSet() //VDP
       {
           CommodityVolParameter temp = new CommodityVolParameter();
           return  (temp == null) ? new CommodityVolParameter() : temp ;
       }

       [ComVisible(false)]
       public List<double> GetCommodityStrikesGrid(bool forceGridInPercentage)
       {
          List<double> outputList = new List<double>();

          CommodityVolComponent baseComponent = null;
            baseComponent = null;
           
            CommodityVolParameter commoVolParameter = this.GetCommodityVolParametersSet();
           if (commoVolParameter == null)   
           {
               return outputList; 
           }
           else if (baseComponent != null && baseComponent.Name == "Vol By delta" && !forceGridInPercentage)
           {
               return commoVolParameter.GetStrikesByDelta();
           }
           else if (commoVolParameter.StrikesGridAsFather && Parents != null && Parents.Count == 1)
           {
               var parent = Parents[0];
               commoVolParameter = parent.GetCommodityVolParametersSet();
           }


           fillstrikesPct(outputList, commoVolParameter); 
           
           return outputList; 
          
       }

        private void fillstrikesPct(List<double> outputList,  CommodityVolParameter commoVolParameter) {

            if (outputList == null) outputList = new List<double>();
            else outputList.Clear();

            // minimum value to prevent wrong imput 
            // by construction it is monotonic and there are not double strikes
            //  in addition starts at GridATMMin ends at GridATMMax and contains both GridATMMin and GridATMMax 

            double wingStep = Math.Max(commoVolParameter.GridWingStep, 0.0001);
            double atmStep = Math.Max(commoVolParameter.GridATMStep, 0.0001);
            double strike = commoVolParameter.GridWingMin;  
            outputList.Add(strike); // wing min added
            strike += wingStep;
            strike = Math.Round(strike, 4); // avoid numerical issues 
            while (strike < commoVolParameter.GridATMMin)
            {
                outputList.Add(strike);
                strike += wingStep;
                strike = Math.Round(strike, 4); // avoid numerical issues 
            }
            strike = commoVolParameter.GridATMMin;  
            outputList.Add(strike); // atm min added
            strike += atmStep;
            strike = Math.Round(strike, 4); // avoid numerical issues 
            while (strike < commoVolParameter.GridATMMax)
            {
                outputList.Add(strike);
                strike += atmStep;
                strike = Math.Round(strike, 4); // avoid numerical issues 
            }
            strike = commoVolParameter.GridATMMax;  
            outputList.Add(strike); // atm max added
            strike += wingStep;
            strike = Math.Round(strike, 4); // avoid numerical issues 
            while (strike < commoVolParameter.GridWingMax)
            {
                outputList.Add(strike);
                strike += wingStep;
                strike = Math.Round(strike, 4); // avoid numerical issues
            }
            strike = commoVolParameter.GridWingMax;
            outputList.Add(strike); // wing max added
            


        }
       
       [ComVisible(false)]
       public List<double> GetCommodityStrikesGridInPercentage()
       {
           List<double> outputList = new List<double>();

           CommodityVolComponent baseComponent = null;
            baseComponent = null;

           CommodityVolParameter commoVolParameter = this.GetCommodityVolParametersSet();
           if (commoVolParameter == null)
           {
               return outputList;
           }
           else if (commoVolParameter.StrikesGridAsFather && Parents != null && Parents.Count == 1)
           {
               var parent = Parents[0];
               commoVolParameter = parent.GetCommodityVolParametersSet();
           }


           fillstrikesPct(outputList, commoVolParameter); 
           
           return outputList;

       }
       

       public Underlyings Underlyings
       {
           get
           {
               List<Underlying> retval = new List<Underlying>();
               var dict = new Dictionary<Int32, bool>();
              
               foreach (UnderlyingClassification uc in Engine.Instance.Factory.GetUnderlyingClassifications(this))
               {
                   if (!dict.ContainsKey(uc.Underlying.Id))
                   {
                       retval.Add(uc.Underlying);
                       dict.Add(uc.UnderlyingId, true);
                   }
               }
               return new Underlyings(retval);
           }
       }

       public IssuanceParameters IssuanceParameters
       {
           get
           {
               List<IssuanceParameter> retval = new List<IssuanceParameter>();
               foreach (IssuanceParameter param in Engine.Instance.Factory.GetIssuanceParameters(this))
               {
                   retval.Add(param);
               }
               return new IssuanceParameters(retval);
           }
       }

       public override string ToString()
       {
           return Name;
       }

       public string Path
       {
           get
           {
               return Engine.Instance.Factory.GetClassificationPath(Id);
           }
       }

    }
}
    