using System;

namespace Puma.MDE.Data
{
    public class HVBMassAuditChangeID
    {
        private int id;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private string sophisUser;

        public string SophisUser
        {
            get { return sophisUser; }
            set { sophisUser = value; }
        }
        private DateTime timeStamp;

        public DateTime TimeStamp
        {
            get { return timeStamp; }
            set { timeStamp = value; }
        }
        private String type;

        public String Type
        {
            get { return type; }
            set { type = value; }
        }


    }
}
