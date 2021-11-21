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
        public async Task<IActionResult> Index(long id)
        {
            var user = DB.Users.GetByID(id);
            await DB.Users.UpdateByAdminAsync(user, true);
            AddInfo("User updated");
            return RedirectToAction("Index");
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
        public async Task<IActionResult> Update(User user)
        {
            await DB.Users.UpdateByAdminAsync(user, false);
            AddInfo("User updated");
            return RedirectToAction("Index");
        }

        public IActionResult Delete()
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            var usr = DB.Users.GetByID(id);
            await DB.Users.LockUserAsync(usr);
            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "delete_user",
                ExecutedById = GetSessionUser().ID,
                Id_Variable = usr.ID
            });
            return RedirectToAction("Index");
        }
    }
}
