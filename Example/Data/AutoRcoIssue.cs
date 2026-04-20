using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    //[ClassInterface(ClassInterfaceType.AutoDual)]
    // [Guid("C96D7BA2-F93C-4516-B92F-6D325692C629")]
    public class RcoEODIssues : IEnumerable
    {
        IList<RcoEODIssue> collection;
        public RcoEODIssues(IList<RcoEODIssue> collection)
        {
            this.collection = collection;
        }
        public RcoEODIssues( )
        {
            this.collection = new List<RcoEODIssue>();
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(RcoEODIssue item)
        {
            collection.Add(item);
        }
        public void AddRange(IEnumerable<RcoEODIssue> range)
        {
            foreach (var item in range)
                { Add(item); }
        }
        public void Clear()
        {
            collection.Clear();
        }
        public RcoEODIssue this[int index]
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

        public void AddRcoEODIssue(Underlying underlying, string description, double? strike, DateTime? maturity, RcoEODIssueType issueType)
        {
            RcoEODIssue rcoEODIssue = new RcoEODIssue();
            
            rcoEODIssue.Underlying  = underlying;
            rcoEODIssue.Description = description;
            rcoEODIssue.Strike      = strike;
            rcoEODIssue.Bucket      = maturity;
            rcoEODIssue.Status      = issueType;
            
            this.Add(rcoEODIssue);
            Engine.Instance.Log.Info(description);
        }

    }

    public enum RcoEODIssueType
    {
        Pass    = 0,
        Outlier = 1,
        FoRco   = 2,
        Vega    = 3,
        Generic = 4
    }

    //[ClassInterface(ClassInterfaceType.AutoDual)]
    //[Guid("A29B7771-04C0-4300-AEC8-8F6536AACE15")]
    public class RcoEODIssue 
    {

        public RcoEODIssue()
        {
            Bucket      = null;
            Underlying  = null;
            Strike      = null;
            Description = "";
            Date        = DateTime.Now;
        }

        public int              Id      { get; set; }
        public DateTime?        Bucket  { get; set; }
        public double?          Strike  { get; set; }
        public DateTime         Date    { get; set; }
        public RcoEODIssueType  Status  { get; set; }

        public string UnderlyingRef 
        {
            get
            {
                if (Underlying == null)
                {
                    return "";
                }
                return Underlying.Reference;
            }
        }
        
        string _description;
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                if (value == null)
                    _description = null;
                else
                {
                    if (value.Length > 256)
                        _description = value.Substring(0, 256);
                    else
                        _description = value;
                }
            }
        }
        
        public Underlying Underlying { get; set; }

        public RcoEODIssue Clone()
        {
            RcoEODIssue retval  = new RcoEODIssue();

            retval.Bucket       = Bucket;
            retval.Strike       = Strike;
            retval.Description  = Description;
            retval.Date         = Date;
            retval.Underlying   = Underlying;
            retval.Status       = Status;
            
            return retval;
        }

    }
}
