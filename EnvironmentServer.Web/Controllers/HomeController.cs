using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Repositories;
using EnvironmentServer.Web.Attributes;
using EnvironmentServer.Web.Models;
using EnvironmentServer.Web.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    public class HomeController : ControllerBase
    {
        private readonly Database DB;

        public HomeController(Database database)
        {
            DB = database;
        }

        public IActionResult Index()
        {
            if (PasswordHasher.Verify("darkstar", GetSessionUser().Password))
            {
                AddError("Please change your Passwort! Do not use darkstar as password!");
                return RedirectToAction("Index", "Profile");
            }

            var dash = new DashboardModel() { Environments = DB.Environments.GetForUser(GetSessionUser().ID), 
                Htaccess = DB.Settings.Get("pma_htacces_login").Value };
            return View(dash);
        }

        [AdminOnly]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
