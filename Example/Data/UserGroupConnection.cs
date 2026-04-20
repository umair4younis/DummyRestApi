using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(false)]
    public class UserGroupConnection
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int UserId { get; set; }
    }
}
