using Puma.MDE.Common;
using System;

namespace Puma.MDE.Data
{
    [Serializable]
    public class QuixReweight : Entity
    {
        public QuixReweight()
        {
            User = Engine.Instance.ConnectedUser;
            Timestamp = DateTime.Now;
        }

        public int UserId { get; set; }

        public User User
        {
            get
            {
                return Engine.Instance.Factory.GetUser(UserId);
            }
            set
            {
                UserId = value.Id;
            }
        }


        public int UnderlyingId { get; set; }

        public Underlying Underlying
        {
            get
            {
                return Engine.Instance.Factory.GetUnderlying(UnderlyingId);
            }
            set
            {
                UnderlyingId = value.Id;
            }
        }

        public int QuixReweightModelId { get; set; }

        public QuixReweightModel QuixReweightModel
        {
            get
            {
                return Engine.Instance.Factory.GetQuixReweightModel(QuixReweightModelId);
            }
            set
            {
                QuixReweightModelId = value.Id;
            }
        }


        public DateTime Timestamp { get; set; }
    }
}
