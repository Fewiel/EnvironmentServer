﻿using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Attributes;
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
        public ProfileController(Database db) : base(db) { }

        public IActionResult Index()
        {
            var usr = DB.Users.GetByID(GetSessionUser().ID);
            var uInfo = DB.UserInformation.Get(usr.ID);

            var pvm = new ProfileViewModel
            {
                SSHPublicKey = usr.SSHPublicKey,
                UserInformation = uInfo,
                UserDepartment = DB.Department.Get(uInfo.DepartmentID)
            };
            return View(pvm);
        }

        [Permission("ssh_key_set")]
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

        [Permission("ssh_key_set")]
        public IActionResult ChangeSSH([FromForm] ProfileViewModel pvm)
        {
            if (string.IsNullOrEmpty(pvm.SSHPublicKey) || !Regex.Match(pvm.SSHPublicKey, "ssh-rsa AAAA[0-9A-Za-z+/]+[=]{0,3}( [^@]+@[^@]+)?").Success)
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
            if (pvm == null)
                return RedirectToAction("Index", "Profile");

            if (!ModelState.IsValid)
                return RedirectToAction("Index", "Profile");

            var usr = GetSessionUser();

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
                IsAdmin = usr.IsAdmin,
                RoleID = usr.RoleID,
                ForcePasswordReset = false
            };

            await DB.Users.UpdateAsync(update_usr, pvm.PasswordNew);

            AddInfo("Password changed");
            return RedirectToAction("Logout", "Login");
        }

        public IActionResult UpdateInformations([FromForm] ProfileViewModel pvm)
        {
            pvm.UserInformation.UserID = GetSessionUser().ID;
            pvm.UserInformation.PrepareForDB();
            DB.UserInformation.Update(pvm.UserInformation);
            AddInfo("Userinformation updated");
            return RedirectToAction("Index", "Profile");
        }

        [AllowNotLoggedIn]
        public IActionResult PasswordRecovery() => View();

        [AllowNotLoggedIn, HttpPost]
        public IActionResult PasswordRecovery([FromForm] PasswordRecoveryViewModel prv)
        {
            DB.Logs.Add("Web", "Password Recovery for: " + prv.Mail);
            DB.Users.ForgotPassword(prv.Mail);
            AddInfo("Passwort recovery mail send. Check your mailbox!");
            return View();
        }

        [AllowNotLoggedIn]
        public IActionResult SetPassword(string token, string mail)
        {
            var prv = new PasswordRecoveryViewModel { Token = token, Mail = mail };
            return View(prv);
        }

        [AllowNotLoggedIn, HttpPost]
        public async Task<IActionResult> SetPasswordAsync([FromForm] PasswordRecoveryViewModel prv)
        {
            if (prv.PasswordNew != prv.PasswordNewRetype)
            {
                AddError("Passwords does not match");
                return View(prv);
            }


            if (!await DB.Users.ResetPasswordAsync(prv.Token, prv.Mail, prv.PasswordNew))
            {
                AddError("Token or Mailaddress not valid");
                return View(prv);
            }

            AddInfo("Passwort set. You can now login to your account!");
            return RedirectToAction("Index", "Home");
        }

        [Permission("cronjobs"), HttpPost]
        public IActionResult ReloadCronjobs()
        {
            var usr = DB.Users.GetByID(GetSessionUser().ID);

            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "reload_cronjobs",
                Id_Variable = usr.ID,
                ExecutedById = usr.ID
            });

            AddInfo("Reloading CornJobs... This can take up to 1 minute");
            return RedirectToAction("Index", "Home");
        }
    }
}
