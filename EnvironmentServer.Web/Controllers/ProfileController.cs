using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.ViewModels.Profile;
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
            AddError("If you change your Password - Please keep in mind: Login Password = Database/FTP/SSH Password - You need to change your DB passwords in all Environments as well. (config.php, .env)");
            return View();
        }

        public async Task<IActionResult> ChangePasswordAsync([FromForm] ProfileViewModel pvm)
        {

            var usr = GetSessionUser();

            if (!ModelState.IsValid)
                return View();

            if (!PasswordHasher.Verify(pvm.Password, usr.Password))
            {
                AddInfo("Wrong password");
                return RedirectToAction("Index", "Profile");
            }

            if (pvm.Password[0] == '#')
            {
                AddError("No special char as first char allowed");
                return View();
            }

            if (pvm.Password.Length <= 6)
            {
                AddError("Password must have at least 6 characters");
                return View();
            }

            if (pvm.PasswordNew != pvm.PasswordNewRetype)
            {
                AddInfo("New password did not match");
                return RedirectToAction("Index", "Profile");
            }

            var update_usr = new User { ID = usr.ID, Username = usr.Username, Email = usr.Email, 
                Password = PasswordHasher.Hash(pvm.PasswordNew), IsAdmin = usr.IsAdmin };

            await DB.Users.UpdateAsync(update_usr, pvm.PasswordNew);

            AddInfo("Password changed");
            return RedirectToAction("Index", "Home");
        }
    }
}
