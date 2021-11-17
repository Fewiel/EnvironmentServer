using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.ViewModels.EnvSetup;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    public class EnvSetupController : ControllerBase
    {
        private readonly Database DB;
        public EnvSetupController(Database db)
        {
            DB = db;
        }

        public IActionResult BaseData(EnvSetupViewModel esv) => View(esv ?? new());

        [HttpPost]
        public IActionResult MajorVersion([FromForm] EnvSetupViewModel esv)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("BaseData", esv);

            var tmp_env_list = DB.Environments.GetForUser(GetSessionUser().ID);

            foreach (var i in tmp_env_list)
            {
                if (i.Name.ToLower() == esv.Name.ToLower())
                {
                    AddError("Environment Name already in use");
                    return RedirectToAction("BaseData", esv);
                }
            }

            return View(esv);
        }

        [HttpPost]
        public IActionResult MinorVersion([FromForm] EnvSetupViewModel esv)
        {
            if (esv.CustomSetupType == "empty")
                RedirectToAction(nameof(PhpVersion), esv);
            if (esv.CustomSetupType == "git")
                RedirectToAction(nameof(GitSource), esv);
            if (esv.CustomSetupType == "wget")
                RedirectToAction(nameof(WGetSource), esv);

            esv.ShopwareVersions = DB.TagCache.GetForMajor(esv.MajorShopwareVersion);
            return View(esv);
        }

        public IActionResult WGetSource(EnvSetupViewModel esv) => View(esv);

        public IActionResult GitSource(EnvSetupViewModel esv) => View(esv);

        [HttpPost]
        public IActionResult PhpVersion([FromForm] EnvSetupViewModel esv)
        {
            return View(esv);
        }

        [HttpPost]
        public async Task<IActionResult> FinalizeAsync([FromForm] EnvSetupViewModel esv)
        {            
            var environment = new Environment()
            {
                UserID = GetSessionUser().ID,
                Name = esv.Name,
                Address = esv.Name.ToLower() + "." + GetSessionUser().Username + "." + DB.Settings.Get("domain").Value,
                Version = esv.PhpVersion
            };

            var lastID = await DB.Environments.InsertAsync(environment, GetSessionUser(),
                esv.MajorShopwareVersion == 6).ConfigureAwait(false);

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
                Value = esv.MajorShopwareVersion.ToString()
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

            return View(esv);

            //if (!string.IsNullOrEmpty(esv.File) && System.Uri.IsWellFormedUriString(esv.File, System.UriKind.RelativeOrAbsolute))
            //{
            //    System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{esv.EnvironmentName}/dl.txt", esv.File);

            //    DB.CmdAction.CreateTask(new CmdAction
            //    {
            //        Action = "download_extract",
            //        Id_Variable = lastID,
            //        ExecutedById = GetSessionUser().ID
            //    });
            //    DB.Environments.SetTaskRunning(lastID, true);
            //}

            //return RedirectToAction("Index", "Home");
        }
    }
}
