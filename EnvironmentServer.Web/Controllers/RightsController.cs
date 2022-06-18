using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Models;
using EnvironmentServer.Web.ViewModels.Rights;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace EnvironmentServer.Web.Controllers
{
    public class RightsController : ControllerBase
    {
        private static Database DB;

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
                Role = new(),
                Permissions = perm.Select(p => WebPermission.FromPermission(p)),
                Limits = limits.Select(limits => WebLimit.FromLimit(limits))
            };

            return View(rvm);
        }

        [HttpPost]
        public IActionResult AddRole(RoleViewModel rvm)
        {
            rvm.Role.ID = DB.Role.Add(new Role() { Name = rvm.Role.Name, Description = rvm.Role.Description });

            foreach (var p in rvm.Permissions)
            {
                if (p.Enabled)
                    DB.Permission.Add(p.ToPermission());
            }

            foreach (var l in rvm.Limits)
            {
                if (l.Value != -1)
                {
                    var limit = l.ToLimit();
                    var rLimit = new RoleLimit()
                    {
                        LimitID = limit.ID,
                        RoleID = rvm.Role.ID,
                        Value = l.Value
                    };

                    DB.RoleLimit.Add(rLimit);
                }
            }

            return View(rvm);
        }
    }
}