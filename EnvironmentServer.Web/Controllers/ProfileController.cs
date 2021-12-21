using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.ViewModels.Profile;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        public async Task<IActionResult> VerifySSHAsync(string token)
        {
            var usr = GetSessionUser();            
            if (usr == null || string.IsNullOrEmpty(token) || !Guid.TryParse(token, out Guid guid))
                return RedirectToAction("Index", "Home");

            if (!DB.Tokens.Use(guid, usr.ID))
            {
                AddError("Invalid token!");
                return RedirectToAction("Index", "Home");
            }

            await DB.Users.SetSSHKeyAsync(usr);
            AddInfo("SSH key was successfully inserted. You can now log in to SSH via SSH key.");
            return RedirectToAction("Index", "Home");
        }
        public IActionResult ChangeSSH([FromForm] ProfileViewModel pvm)
        {
            if (string.IsNullOrEmpty(pvm.SSHPublicKey) || !Regex.Match(pvm.SSHPublicKey, "ssh-rsa AAAA[0-9A-Za-z+/]+[=]{0,3} ([^@]+@[^@]+)").Success)
            {
                AddError("Please enter valid SSH Key - Use OpenSSH format e.g. \"ssh-rsa AAAA...\"");
                return RedirectToAction("Index", "Profile");
            }

            var usr = GetSessionUser();
            DB.Users.UpdateSSHKey(pvm.SSHPublicKey, usr.ID);
            DB.Logs.Add("Web", "Change SSH Public Key for : " + usr.Username);
            DB.Users.SendSSHConfirmation(usr);
            AddInfo("SSH Key Updated - Please check your mail to confirm your action!");
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> ChangePasswordAsync([FromForm] ProfileViewModel pvm)
        {

            var usr = GetSessionUser();

            if (!ModelState.IsValid)
                return RedirectToAction("Index", "Profile");

            if (!PasswordHasher.Verify(pvm.Password, usr.Password))
            {
                AddInfo("Wrong password");
                return RedirectToAction("Index", "Profile");
            }

            if (pvm.PasswordNew.Length < 6)
            {
                AddError("Password must have at least 6 characters");
                return RedirectToAction("Index", "Profile");
            }

            if (pvm.PasswordNew != pvm.PasswordNewRetype)
            {
                AddInfo("New password did not match");
                return RedirectToAction("Index", "Profile");
            }

            var update_usr = new User
            {
                ID = usr.ID,
                Username = usr.Username,
                Email = usr.Email,
                Password = PasswordHasher.Hash(pvm.PasswordNew),
                IsAdmin = usr.IsAdmin
            };

            await DB.Users.UpdateAsync(update_usr, pvm.PasswordNew);

            AddInfo("Password changed");
            return RedirectToAction("Logout", "Login");
        }
    }
}
