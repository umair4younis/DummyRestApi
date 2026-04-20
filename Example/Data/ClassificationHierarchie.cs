using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("E371132F-4BD4-4b99-985C-AF460DBA0306")]
    [ComVisible(true)]

    public class ClassificationHierarchie
    {
        
        public int Id {get;set;}
        public int ParentId {get;set;}
        public int ChildId {get;set;}

        public Classification Child
        {
            get
            {
                return Engine.Instance.Factory.GetClassification(ChildId);
            }
        }
        public Classification Parent
        {
            get
            {
                return Engine.Instance.Factory.GetClassification(ParentId);
            }
        }
        
    }

}
