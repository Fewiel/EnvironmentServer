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
        public HomeController(Database database) : base(database) { }

        public IActionResult Index()
        {
            if (PasswordHasher.Verify("darkstar", GetSessionUser().Password))
            {
                AddError("Please change your Passwort! Do not use darkstar as password!");
                return RedirectToAction("Index", "Profile");
            }
                        
            var dash = new DashboardModel()
            {
                Environments = DB.Environments.GetForUser(GetSessionUser().ID),
                Htaccess = DB.Settings.Get("pma_htacces_login").Value
            };

            if (DB.Permission.HasPermission(GetSessionUser(), "admin_statistics_show"))
            {
                dash.PerformanceData = DB.Performance.Get();
                dash.Queue = DB.Performance.GetQueue();
                dash.UserCount = DB.Performance.GetUsers();
                dash.EnvironmentCount = DB.Performance.GetEnvironments();
                dash.StoredCount = DB.Performance.GetStoredEnvironments();
            }

            DB.Users.UpdateLastUse(GetSessionUser());

            if (DB.Users.GetByUsername(GetSessionUser().Username) == null || !DB.Users.GetByUsername(GetSessionUser().Username).Active)
                HttpContext.Session.Clear();

            return View(dash);
        }

        public IActionResult Pma()
        {
            return Redirect("https://" + DB.Settings.Get("pma_htacces_login").Value + "@" + DB.Settings.Get("pma_link").Value);
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
