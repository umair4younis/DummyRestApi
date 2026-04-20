using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(false)]
    public class UserRight
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int ClassificationId { get; set; }
        public int PermissionTypeId { get; set; }
        public bool Permission { get; set;}


        public string PermissionName
        {
            get
            {
                return Engine.Instance.Factory.GetPermissionType(PermissionTypeId).Name;
            }
        }
        public string GroupName
        {
            get
            {
                return Engine.Instance.Factory.GetUserGroup(GroupId).Name;
            }
        }
    }
}
