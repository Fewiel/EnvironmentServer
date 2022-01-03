using EnvironmentServer.DAL.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace EnvironmentServer.Web.ViewModels.Users;

public class AdminUsersViewModel
{
    public User User { get; set; }
    public IEnumerable<SelectListItem> DepartmentList { get; set; }
}
