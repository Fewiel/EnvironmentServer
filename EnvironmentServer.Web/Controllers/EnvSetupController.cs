using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.ViewModels.EnvSetup;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
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
            if (!ModelState.IsValid || string.IsNullOrEmpty(esv.InternalName))
                return RedirectToAction("BaseData", esv);

            if (esv.InternalName != DAL.Repositories.EnvironmentRepository.FixEnvironmentName(esv.InternalName))
            {
                esv.InternalName = DAL.Repositories.EnvironmentRepository.FixEnvironmentName(esv.InternalName);
                AddError("Your environment name was fixed - No spaces and capital letters allowed");
                return RedirectToAction("BaseData", esv);
            }

            if (string.IsNullOrEmpty(esv.InternalName))
            {
                AddError("Please enter a name - lower case, no special chars, no spaces");
                return RedirectToAction("BaseData", esv);
            }

            var tmp_env_list = DB.Environments.GetForUser(GetSessionUser().ID);

            foreach (var i in tmp_env_list)
            {
                if (i.InternalName.ToLower() == esv.InternalName.ToLower())
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
                return RedirectToAction(nameof(PhpVersion), esv);
            if (esv.CustomSetupType == "git")
                return RedirectToAction(nameof(GitSource), esv);
            if (esv.CustomSetupType == "wget")
                return RedirectToAction(nameof(WGetSource), esv);
            if (esv.CustomSetupType == "exhibition")
                return RedirectToAction(nameof(Exhibition), esv);

            esv.ShopwareVersions = DB.ShopwareVersionInfos.GetForMajor(esv.MajorShopwareVersion);
            return View(esv);
        }

        public IActionResult WGetSource(EnvSetupViewModel esv) => View(esv);

        public IActionResult GitSource(EnvSetupViewModel esv) => View(esv);

        public IActionResult PhpVersion(EnvSetupViewModel esv)
        {
            esv.PhpVersions = System.Enum.GetValues(typeof(PhpVersion)).Cast<PhpVersion>();
            return View(esv);
        }

        public IActionResult Exhibition(EnvSetupViewModel esv)
        {
            esv.ExhibitionVersions = DB.ExhibitionVersion.Get();
            return View(esv);
        }

        [HttpPost]
        public IActionResult Finalize([FromForm] EnvSetupViewModel esv)
        {
            return View(esv);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm] EnvSetupViewModel esv)
        {
            if (!string.IsNullOrEmpty(esv.ExhibitionFile))
            {
                esv.ShopwareVersion = "6";
                esv.MajorShopwareVersion = 6;
            }

            var environment = new Environment()
            {
                UserID = GetSessionUser().ID,
                DisplayName = esv.DisplayName,
                InternalName = esv.InternalName,
                Address = esv.InternalName.ToLower() + "-" + GetSessionUser().Username + "." + DB.Settings.Get("domain").Value,
                Version = esv.PhpVersion
            };

            if (esv.MajorShopwareVersion == 0)
                esv.ShopwareVersion = "Custom";

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
                Value = esv.ShopwareVersion
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

            if (!string.IsNullOrEmpty(esv.ExhibitionFile))
            {
                System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{esv.InternalName}/dl.txt", esv.ExhibitionFile);

                DB.CmdAction.CreateTask(new CmdAction
                {
                    Action = "setup_exhibition",
                    Id_Variable = lastID,
                    ExecutedById = GetSessionUser().ID
                });
                DB.Environments.SetTaskRunning(lastID, true);
            }

            if (!string.IsNullOrEmpty(esv.WgetURL) && System.Uri.IsWellFormedUriString(esv.WgetURL, System.UriKind.RelativeOrAbsolute))
            {
                System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{esv.InternalName}/dl.txt", esv.WgetURL);

                DB.CmdAction.CreateTask(new CmdAction
                {
                    Action = "download_extract",
                    Id_Variable = lastID,
                    ExecutedById = GetSessionUser().ID
                });
                DB.Environments.SetTaskRunning(lastID, true);
            }

            if (!string.IsNullOrEmpty(esv.ShopwareVersionDownload))
            {
                System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{esv.InternalName}/dl.txt",
                    esv.ShopwareVersionDownload);

                DB.CmdAction.CreateTask(new CmdAction
                {
                    Action = "download_extract",
                    Id_Variable = lastID,
                    ExecutedById = GetSessionUser().ID
                });
                DB.Environments.SetTaskRunning(lastID, true);
            }

            if (!string.IsNullOrEmpty(esv.GitURL) && System.Uri.IsWellFormedUriString(esv.GitURL, System.UriKind.RelativeOrAbsolute))
            {
                System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{esv.InternalName}/dl.txt", esv.GitURL);

                DB.CmdAction.CreateTask(new CmdAction
                {
                    Action = "clone_repo",
                    Id_Variable = lastID,
                    ExecutedById = GetSessionUser().ID
                });
                DB.Environments.SetTaskRunning(lastID, true);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> CreateWithAutoinstallerAsync([FromForm] EnvSetupViewModel esv)
        {
            var environment = new Environment()
            {
                UserID = GetSessionUser().ID,
                InternalName = esv.InternalName,
                Address = esv.InternalName.ToLower() + "-" + GetSessionUser().Username + "." + DB.Settings.Get("domain").Value,
                Version = esv.PhpVersion
            };

            if (esv.MajorShopwareVersion == 0)
                esv.ShopwareVersion = "Custom";

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
                Value = esv.ShopwareVersion
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

            if (!string.IsNullOrEmpty(esv.ShopwareVersionDownload))
            {
                System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{esv.InternalName}/dl.txt",
                    esv.ShopwareVersionDownload);

                DB.CmdAction.CreateTask(new CmdAction
                {
                    Action = "download_extract_autoinstall",
                    Id_Variable = lastID,
                    ExecutedById = GetSessionUser().ID
                });
                DB.Environments.SetTaskRunning(lastID, true);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}