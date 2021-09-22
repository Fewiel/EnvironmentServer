using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
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
        public IActionResult Login() => View();

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

        [HttpGet]
        public IActionResult Registration() => View();

        [HttpPost]
        public async Task<IActionResult> Registration([FromForm]RegistrationViewModel rvm)
        {
            if (!ModelState.IsValid)
                return View();

            if (DB.Users.GetByUsername(rvm.Username) != null)
            {
                AddError("Username already taken.");
                return View();
            }

            var usr = new User { Username = rvm.Username, Email = rvm.Email, Password = PasswordHasher.Hash(rvm.Password) };
            await DB.Users.InsertAsync(usr, rvm.Password).ConfigureAwait(false);

            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToRoute("login");
        }
    }
}
