﻿using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Attributes;
using EnvironmentServer.Web.ViewModels.EnvSetup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    [Permission("environment_create")]
    public class EnvSetupController : ControllerBase
    {
        public EnvSetupController(Database db) : base(db) { }

        public IActionResult BaseData(EnvSetupViewModel esv)
        {
            var environments = DB.Environments.GetForUser(GetSessionUser().ID);
            var usr = DB.Users.GetByID(GetSessionUser().ID);
            var limit = DB.Limit.GetLimit(usr, "environment_max");

            if (limit <= environments.Count() && limit > 0)
            {
                AddError($"You have to many Environments! You can only have {limit} Environment(s)!");
                return RedirectToAction("Index", "Home");
            }

            return View(esv ?? new());
        }

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
                if (string.Equals(i.InternalName, esv.InternalName, System.StringComparison.OrdinalIgnoreCase))
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
                return RedirectToAction(nameof(EmptyWebspaceSettings), esv);
            if (esv.CustomSetupType == "git")
                return RedirectToAction(nameof(GitSource), esv);
            if (esv.CustomSetupType == "wget")
                return RedirectToAction(nameof(WGetSource), esv);
            if (esv.CustomSetupType == "exhibition")
                return RedirectToAction(nameof(Exhibition), esv);
            if (esv.CustomSetupType == "template")
                return RedirectToAction(nameof(Template), esv);

            esv.ShopwareVersions = DB.ShopwareVersionInfos.GetForMajor(esv.MajorShopwareVersion);
            return View(esv);
        }

        [HttpPost]
        public async Task<IActionResult> MinorVersion6Async([FromForm] EnvSetupViewModel esv)
        {
            if (esv.CustomSetupType == "empty")
                return RedirectToAction(nameof(EmptyWebspaceSettings), esv);
            if (esv.CustomSetupType == "git")
                return RedirectToAction(nameof(GitSource), esv);
            if (esv.CustomSetupType == "wget")
                return RedirectToAction(nameof(WGetSource), esv);
            if (esv.CustomSetupType == "exhibition")
                return RedirectToAction(nameof(Exhibition), esv);
            if (esv.CustomSetupType == "template")
                return RedirectToAction(nameof(Template), esv);

            esv.Shopware6Versions = await DB.Shopware6Version.GetVersionsAsync();
            return View(esv);
        }

        public IActionResult EmptyWebspaceSettings(EnvSetupViewModel esv) => View(esv);

        [HttpPost]
        public IActionResult EmptySW5Route(EnvSetupViewModel esv)
        {
            esv.MajorShopwareVersion = 5;
            return RedirectToAction(nameof(PhpVersion), esv);
        }

        [HttpPost]
        public IActionResult EmptySW6Route(EnvSetupViewModel esv)
        {
            esv.MajorShopwareVersion = 6;
            return RedirectToAction(nameof(PhpVersion), esv);
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

        public IActionResult Template(EnvSetupViewModel esv)
        {
            esv.Templates = DB.Templates.GetAllSorted();
            return View(esv);
        }

        [HttpPost]
        public IActionResult Finalize([FromForm] EnvSetupViewModel esv)
        {
            esv.Languages = System.Enum.GetValues(typeof(Language)).Cast<Language>()
                    .Select(v => new SelectListItem(v.AsText(), ((int)v).ToString()));
            esv.Currencies = System.Enum.GetValues(typeof(Currency)).Cast<Currency>()
                    .Select(v => new SelectListItem(v.AsText(), ((int)v).ToString()));
            return View(esv);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm] EnvSetupViewModel esv)
        {
            DB.Logs.Add("Debug", "EnvSetupViewModel: " + JsonConvert.SerializeObject(esv));

            if (!string.IsNullOrEmpty(esv.ExhibitionFile) || esv.TemplateID != 0 || esv.Shopware6Versions != null)
            {
                esv.ShopwareVersion = "6";
                esv.MajorShopwareVersion = 6;
            }

            if (esv.CustomSetupType == "empty")
            {
                esv.ShopwareVersion = "N/A";
            }

            var environment = new DAL.Models.Environment()
            {
                UserID = GetSessionUser().ID,
                DisplayName = esv.DisplayName,
                InternalName = esv.InternalName,
                Address = esv.InternalName.ToLower() + "-" + GetSessionUser().Username + "." + DB.Settings.Get("domain").Value,
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

            if (!string.IsNullOrEmpty(esv.Shopware6VersionDownload))
            {
                System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{esv.InternalName}/version.txt",
                    esv.Shopware6VersionDownload);

                DB.CmdAction.CreateTask(new CmdAction
                {
                    Action = "clone_production_template",
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

            if (esv.TemplateID != 0)
            {
                System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{esv.InternalName}/template.txt",
                    esv.TemplateID.ToString());

                DB.CmdAction.CreateTask(new CmdAction
                {
                    Action = "fast_deploy",
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
            var environment = new DAL.Models.Environment()
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

            if (!string.IsNullOrEmpty(esv.Shopware6VersionDownload))
            {
                System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{esv.InternalName}/version.txt",
                    esv.Shopware6VersionDownload);

                System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{esv.InternalName}/default-settings.txt",
                    $"{((Language)esv.Language).AsCode()}:{((Currency)esv.Currency).AsCode()}");

                DB.CmdAction.CreateTask(new CmdAction
                {
                    Action = "clone_production_template_install",
                    Id_Variable = lastID,
                    ExecutedById = GetSessionUser().ID
                });
                DB.Environments.SetTaskRunning(lastID, true);
            }

            if (!string.IsNullOrEmpty(esv.ShopwareVersionDownload))
            {
                System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{esv.InternalName}/dl.txt",
                    esv.ShopwareVersionDownload);

                System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{esv.InternalName}/default-settings.txt",
                   $"{((Language)esv.Language).AsCode()}:{((Currency)esv.Currency).AsCode()}");

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

    public enum Language
    {
        EN,
        DE
    }

    public enum Currency
    {
        USD,
        EUR
    }

    public static class LanguageExtensions
    {
        public static string AsText(this Language v) => v switch
        {
            Language.EN => "Englisch",
            Language.DE => "German",
            _ => throw new InvalidOperationException("Unkown Php Version: " + v)
        };

        public static string AsCode(this Language v) => v switch
        {
            Language.EN => "en_US",
            Language.DE => "de_DE",
            _ => throw new InvalidOperationException("Unkown Php Version: " + v)
        };
    }

    public static class CurrencyExtensions
    {
        public static string AsText(this Currency v) => v switch
        {
            Currency.USD => "US Dollar",
            Currency.EUR => "Euro",
            _ => throw new InvalidOperationException("Unkown Php Version: " + v)
        };

        public static string AsCode(this Currency v) => v switch
        {
            Currency.USD => "USD",
            Currency.EUR => "EUR",
            _ => throw new InvalidOperationException("Unkown Php Version: " + v)
        };
    }
}