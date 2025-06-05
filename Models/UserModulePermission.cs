namespace MiniAccountManagementSystem.Models
{
    public class UserModulePermission
    {
        public int PermissionId { get; set; }
        public string RoleName { get; set; }
        public string ModuleName { get; set; }
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
