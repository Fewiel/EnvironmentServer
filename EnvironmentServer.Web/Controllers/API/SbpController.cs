using EnvironmentServer.DAL;
using EnvironmentServer.Web.Models.API;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System;
using System.Threading.Tasks;
using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;

namespace EnvironmentServer.Web.Controllers.API
{
    public class SbpController : Controller
    {
        private Database DB;

        public SbpController(Database db)
        {
            DB = db;
        }

        private string CreatePassword(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        private string CreateEnvName(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyz";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        [HttpPost]
        public async Task<CreateEnvironmentResponse> CreateAsync(CreateEnvironment ce)
        {
            Request.Headers.TryGetValue("authorization", out var apikey);

            if (ce == null)
                return null;

            if (string.IsNullOrEmpty(apikey) || apikey != DB.Settings.Get("sbp_api_key").Value)
                return null;

            if (ce.AccountID == 0)
                return null;

            var usr = DB.Users.GetByUsername("ext" + ce.AccountID);
            if (usr == null)
            {
                usr = new User
                {
                    Active = true,
                    Email = ce.AccountMail,
                    IsAdmin = false,
                    ID = ce.AccountID,
                    RoleID = 1,
                    Username = "ext" + ce.AccountID,
                    LastUsed = DateTime.Now,
                    UserInformation = new UserInformation
                    {
                        AdminNote = $"SBP Account ID: {ce.AccountID}, Email: {ce.AccountMail}",
                        UserID = ce.AccountID,
                        ID = ce.AccountID
                    },
                    Password = PasswordHasher.Hash(CreatePassword(24)),
                    SSHPublicKey = "",
                    ExpirationDate = null,
                    ForcePasswordReset = false
                };
                await DB.Users.InsertAsync(usr, CreatePassword(24), false);
            }
            else if (ce.AccountMail != usr.Email)
            {
                usr.Email = ce.AccountMail;
                await DB.Users.UpdateAsync(usr, CreatePassword(24));
            }

            var envName = CreateEnvName(8);
            var url = "https://" + envName + "-ext" + ce.AccountID + "." + DB.Settings.Get("domain");
            var version = PhpVersion.Php81;

            if (ce.ShopwareVersion.StartsWith("6.4"))
                version = PhpVersion.Php74;

            var environmentPasswd = CreatePassword(24);
            var lastID = await DB.Environments.InsertAsync(new DAL.Models.Environment
            {
                DisplayName = ce.ExtensionName,
                InternalName = envName,
                Address = url,
                Version = version,
                DevelopmentMode = false,
                DBPassword = environmentPasswd,
                UserID = usr.ID,
                LatestUse = DateTime.Now,
                Permanent = true,
                Sorting = 0,
                Stored = false
            }, usr, true);

            var envSettingPersistent = new EnvironmentSettingValue()
            {
                EnvironmentID = lastID,
                EnvironmentSettingID = 1,
                Value = false.ToString()
            };
            var envSettingTemplate = new EnvironmentSettingValue()
            {
                EnvironmentID = lastID,
                EnvironmentSettingID = 2,
                Value = false.ToString()
            };
            var envSettingSWVersion = new EnvironmentSettingValue()
            {
                EnvironmentID = lastID,
                EnvironmentSettingID = 3,
                Value = ce.ShopwareVersion
            };
            var envSettingTask = new EnvironmentSettingValue()
            {
                EnvironmentID = lastID,
                EnvironmentSettingID = 4,
                Value = false.ToString()
            };

            DB.EnvironmentSettings.Insert(envSettingPersistent);
            DB.EnvironmentSettings.Insert(envSettingTemplate);
            DB.EnvironmentSettings.Insert(envSettingSWVersion);
            DB.EnvironmentSettings.Insert(envSettingTask);

            System.IO.File.WriteAllText($"/home/{usr.Username}/files/{envName}/version.txt",
                    ce.ShopwareVersion);

            System.IO.File.WriteAllBytes($"/home/{usr.Username}/files/{envName}/{ce.ExtensionName}.zip", Convert.FromBase64String(ce.Base64Extension));

            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "extension_testing_install",
                Id_Variable = lastID,
                ExecutedById = usr.ID
            });
            DB.Environments.SetTaskRunning(lastID, true);

            return new CreateEnvironmentResponse { ID = lastID, Password = environmentPasswd, URL = url };
        }

        [HttpPost]
        public bool DeleteAsync(long id)
        {
            Request.Headers.TryGetValue("authorization", out var apikey);

            if (string.IsNullOrEmpty(apikey) || apikey != DB.Settings.Get("sbp_api_key").Value)
                return false;

            if (id == 0 || DB.Environments.Get(id) == null)
                return false;

            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "delete_environment",
                Id_Variable = id,
                ExecutedById = DB.Environments.Get(id).UserID
            });
            DB.Environments.SetTaskRunning(id, true);

            return true;
        }
    }
}
