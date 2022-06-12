using EnvironmentServer.DAL;
using EnvironmentServer.Web.Models;
using EnvironmentServer.Web.ViewModels.Rights;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace EnvironmentServer.Web.Controllers
{
    public class RightsController : Controller
    {
        private Database DB;
        public RightsController(Database db)
        {
            DB = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Roles()
        {
            return View(DB.Role.GetAll());
        }

        public IActionResult AddRole()
        {
            var perm = DB.Permission.GetAll();
            var limits = DB.Limit.GetAll();

            var rvm = new RoleViewModel
            {
                Permissions = perm.Select
            };

            return View()
        }
    }
}
