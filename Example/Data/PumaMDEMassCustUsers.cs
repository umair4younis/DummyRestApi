namespace Puma.MDE.Data
{
    public class PumaMDEMassCustUsers
    {
        private string userID;

        public string UserID
        {
            get { return userID; }
            set { userID = value; }
        }
        private int mDEUserRight;

        public int MDEUserRight
        {
            get { return mDEUserRight; }
            set { mDEUserRight = value; }
        }
        private UserRight userRight;

        public UserRight UserRight
        {
            get { return userRight; }
            set { userRight = value; }
        }
        private bool isDirty;

        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }
    }
}
