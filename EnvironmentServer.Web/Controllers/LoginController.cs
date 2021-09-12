using EnvironmentServer.DAL;
using EnvironmentServer.Web.Attributes;
using EnvironmentServer.Web.Extensions;
using EnvironmentServer.Web.ViewModels.Login;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    [AllowNotLoggedIn]
    public class LoginController : ControllerBase
    {
        private readonly Database DB;

        public LoginController(Database database)
        {
            DB = database;
        }

        [HttpGet, Route("/Login", Name = "login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost, Route("/Login", Name = "login")]
        public IActionResult Login([FromForm]LoginViewModel lvm)
        {
            if (!ModelState.IsValid)
                return View();

            var usr = DB.Users.GetByUsername(lvm.Username);
            if (usr == null)
            {
                AddError("Wrong username or password");
                return View();
            }

            if (PasswordHasher.Verify(lvm.Password, usr.Password))
            {
                HttpContext.Session.SetObject("user", usr);
                return RedirectToAction("Index", "Home");
            }

            AddError("Wrong username or password");
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToRoute("login");
        }
    }
}
