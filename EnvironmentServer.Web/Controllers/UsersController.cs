using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Attributes;
using EnvironmentServer.Web.ViewModels.Login;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    [AdminOnly]
    public class UsersController : ControllerBase
    {
        private Database DB;

        public UsersController(Database db)
        {
            DB = db;
        }

        public IActionResult Index()
        {
            return View(DB.Users.GetUsers());
        }

        [HttpPost]
        public IActionResult Index(long id)
        {
            var user = DB.Users.GetByID(id);
            DB.Users.UpdateByAdmin(user, true);
            AddInfo("User updated");
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] RegistrationViewModel rvm)
        {
            if (!ModelState.IsValid)
                return View();

            if (DB.Users.GetByUsername(rvm.Username) != null)
            {
                DB.Logs.Add("Web", "Registration failed for: " + rvm.Username + ". Username already taken.");
                AddError("Username already taken.");
                return View();
            }

            var usr = new User { Username = rvm.Username, Email = rvm.Email, Password = PasswordHasher.Hash(rvm.Password) };
            await DB.Users.InsertAsync(usr, rvm.Password).ConfigureAwait(false);

            AddInfo("User created");
            return View();
        }

        public IActionResult Update(long id)
        {
            return View(DB.Users.GetByID(id));
        }

        [HttpPost]
        public IActionResult Update(User user)
        {
            DB.Users.UpdateByAdmin(user, false);
            AddInfo("User updated");
            return RedirectToAction("Index");
        }
    }
}
