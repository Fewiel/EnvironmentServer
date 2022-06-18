using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Attributes;
using EnvironmentServer.Web.Extensions;
using EnvironmentServer.Web.ViewModels.Login;
using EnvironmentServer.Web.ViewModels.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    [Permission("users_manage")]
    public class UsersController : ControllerBase
    {
        public UsersController(Database db) : base(db) { }

        public IActionResult Index()
        {
            return View(DB.Users.GetUsers());
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(long id)
        {
            var user = DB.Users.GetByID(id);
            await DB.Users.UpdateByAdminAsync(user, true);
            AddInfo("User password reseted");
            return RedirectToAction("Index");
        }

        public IActionResult Create()
        {
            var rvm = new RegistrationViewModel();
            rvm.Roles = DB.Role.GetAll().Select(r => new SelectListItem(r.Name, r.ID.ToString())).ToList();

            return View(rvm);
        }

        public async Task<IActionResult> RegenerateAsync()
        {
            await DB.Users.RegenerateConfig();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] RegistrationViewModel rvm)
        {
            rvm.Roles = DB.Role.GetAll().Select(r => new SelectListItem(r.Name, r.ID.ToString())).ToList();

            if (!ModelState.IsValid)
                return View(rvm);

            if (DB.Users.GetByUsername(rvm.Username) != null)
            {
                DB.Logs.Add("Web", "Registration failed for: " + rvm.Username + ". Username already taken.");
                AddError("Username already taken.");
                return View(rvm);
            }

            if (rvm.Password[0] == '#')
            {
                AddError("No special char as first char allowed");
                return View(rvm);
            }

            var usr = new User
            {
                Username = rvm.Username,
                Email = rvm.Email,
                Password = PasswordHasher.Hash(rvm.Password),
                ExpirationDate = rvm.ExpirationDate
            };
            await DB.Users.InsertAsync(usr, rvm.Password).ConfigureAwait(false);

            DB.Logs.Add("Web", "New Registration for: " + rvm.Username + " by " + GetSessionUser().Username);
            AddInfo("User created");
            return View(rvm);
        }

        public IActionResult Update(long id)
        {
            var auvm = new AdminUsersViewModel
            {
                User = DB.Users.GetByID(id),
                DepartmentList = DB.Department.GetAll()
                    .Select(d => new SelectListItem(d.Name, d.ID.ToString())),
                Roles = DB.Role.GetAll().Select(r => new SelectListItem(r.Name, r.ID.ToString())).ToList()
            };
            return View(auvm);
        }

        public IActionResult LoginAsUser(long id)
        {
            DB.Logs.Add("Web", "Admin: " + GetSessionUser().Username + " logged in as " + DB.Users.GetByID(id).Username);
            HttpContext.Session.Clear();
            HttpContext.Session.SetObject("user", DB.Users.GetByID(id));
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromForm] AdminUsersViewModel auvm)
        {
            var usr = DB.Users.GetByID(auvm.User.ID);
            usr.IsAdmin = auvm.User.IsAdmin;
            usr.Email = auvm.User.Email;
            usr.Active = auvm.User.Active;
            usr.ExpirationDate = auvm.User.ExpirationDate;
            await DB.Users.UpdateByAdminAsync(usr, false);
            auvm.User.UserInformation.PrepareForDB();
            DB.UserInformation.Update(auvm.User.UserInformation);
            AddInfo("User updated");
            DB.Logs.Add("Web", "Update for: " + auvm.User.Username + " by " + GetSessionUser().Username);
            return RedirectToAction("Index");
        }

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
            DB.Logs.Add("Web", "Delete User: " + usr.Username + " by " + GetSessionUser().Username);
            return RedirectToAction("Index");
        }

        public IActionResult UpdateChroot()
        {
            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "update_chroot",
                ExecutedById = GetSessionUser().ID,
                Id_Variable = 0
            });
            return RedirectToAction("Index");
        }
    }
}
