using EnvironmentServer.Web.Models;
using System.Collections.Generic;

namespace EnvironmentServer.Web.ViewModels.Users
{
    public class PermissionViewModel
    {
        public long UserID { get; set; }
        public List<WebLimit> Limits { get; set; }
        public List<WebPermission> Permissions { get; set; }
    }
}
