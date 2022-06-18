using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Models;
using System.Collections.Generic;

namespace EnvironmentServer.Web.ViewModels.Rights;

public class RoleViewModel
{
    public Role Role { get; set; }
    public List<WebLimit> Limits { get; set; }
    public List<WebPermission> Permissions { get; set; }
}