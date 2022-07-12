using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Repositories;
using EnvironmentServer.Web.Attributes;
using EnvironmentServer.Web.Extensions;
using EnvironmentServer.Web.ViewModels.Login;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    [AllowNotLoggedIn]
    public class LoginController : ControllerBase
    {
        public LoginController(Database database) : base(database) { }

        [HttpGet, Route("/Login", Name = "login")]
        public IActionResult Login()
        {
            var lvm = new LoginViewModel
            {
                LatestNews = DB.News.GetLatest(2)
            };
            return View(lvm);
        }

        [HttpPost, Route("/Login", Name = "login")]
        public IActionResult Login([FromForm]LoginViewModel lvm)
        {
            Thread.Sleep(300);

            if(lvm == null)
            {
                AddError("Please enter username and password!");
                return RedirectToRoute("login");
            }

            if (!ModelState.IsValid)
            {
                AddError("Please enter a correct username and password!");
                return RedirectToRoute("login");
            }

            var usr = new User();

            if (lvm.Username.Contains("@shopware.com"))
            {
                usr = DB.Users.GetByMail(lvm.Username);
            }
            else
            {
                usr = DB.Users.GetByUsername(lvm.Username);
            }

            if (usr == null)
            {
                DB.Logs.Add("Web", "Login failed for: " + lvm.Username + ". User not found.");
                AddError("Wrong username or password");
                return RedirectToRoute("login");
            }

            if (!usr.Active)
            {
                DB.Logs.Add("Web", "Login failed for: " + lvm.Username + ". User inactive.");
                AddError("Login failed for: " + lvm.Username + ". User inactive. Please contact your Teamlead.");
                return RedirectToRoute("login");
            }

            if (PasswordHasher.Verify(lvm.Password, usr.Password))
            {
                DB.Logs.Add("Web", "User " + lvm.Username + " logged in!");
                HttpContext.Session.SetObject("user", usr);
                return RedirectToAction("Index", "Home");
            }
            DB.Logs.Add("Web", "Login failed for: " + lvm.Username + ". Wrong username or password.");
            AddError("Wrong username or password");
            return RedirectToRoute("login");
        }

        [HttpGet]
        public IActionResult Registration()
        {
            return RedirectToRoute("login");
        }

        [HttpPost]
        public async Task<IActionResult> Registration([FromForm]RegistrationViewModel rvm)
        {
            if (!ModelState.IsValid)
                return View();

            //Issue #9 Check username
            if (DB.Users.GetByUsername(rvm.Username) != null)
            {
                DB.Logs.Add("Web", "Registration failed for: " + rvm.Username + ". Username already taken.");
                AddError("Username already taken.");
                return View();
            }

            var usr = new User { Username = rvm.Username, Email = rvm.Email, Password = PasswordHasher.Hash(rvm.Password) };
            await DB.Users.InsertAsync(usr, rvm.Password).ConfigureAwait(false);

            return RedirectToRoute("login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToRoute("login");
        }
    }
}
