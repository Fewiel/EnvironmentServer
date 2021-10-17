using EnvironmentServer.DAL;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    public class ProfileController : ControllerBase
    {
        private Database DB;
        public ProfileController(Database db)
        {
            DB = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ChangePassword()
        {



            AddInfo("Password changed");
            return RedirectToAction("Index", "Home");
        }
    }
}
