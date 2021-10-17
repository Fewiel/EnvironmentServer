using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Repositories;
using EnvironmentServer.Web.ViewModels.Env;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    public class EnvController : ControllerBase
    {
        private Database DB;

        public EnvController(Database database)
        {
            DB = database;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            var createViewModel = new CreateViewModel()
            {
                PhpVersions = System.Enum.GetValues(typeof(PhpVersion)).Cast<PhpVersion>()
                    .Select(v => new SelectListItem(v.AsString(), ((int)v).ToString()))
            };
            return View(createViewModel);
        }

        [HttpGet]
        public IActionResult Update(long id)
        {
            var createViewModel = new UpdateViewModel()
            {
                ID = id,
                EnvironmentName = DB.Environments.Get(id).Name,
                PhpVersions = System.Enum.GetValues(typeof(PhpVersion)).Cast<PhpVersion>()
                    .Select(v => new SelectListItem(v.AsString(), ((int)v).ToString()))
            };
            return View(createViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Update(long id, [FromForm] UpdateViewModel cvm)
        {

            await DB.Environments.UpdatePhpAsync(id, GetSessionUser(), (PhpVersion)cvm.Version);

            AddInfo("Environment PhpVersion sucessfull updated");
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm] CreateViewModel cvm)
        {
            if (!ModelState.IsValid)
                return View(cvm);

            var tmp_env_list = DB.Environments.GetForUser(GetSessionUser().ID);

            //Issue #10 Check for environment name
            foreach (var i in tmp_env_list)
            {
                if (i.Name.ToLower() == cvm.EnvironmentName.ToLower())
                {
                    AddError("Environment Name already in use");
                    return RedirectToAction("Index", "Home");
                }
            }

            var environment = new Environment()
            {
                UserID = GetSessionUser().ID,
                Name = cvm.EnvironmentName,
                Address = cvm.EnvironmentName + "." + GetSessionUser().Username + ".shopware.env",
                Version = (PhpVersion)cvm.Version
            };

            var lastID = await DB.Environments.InsertAsync(environment, GetSessionUser()).ConfigureAwait(false);

            //Issue #4 Download and extract
            if (!string.IsNullOrEmpty(cvm.File) && System.Uri.IsWellFormedUriString(cvm.File, System.UriKind.RelativeOrAbsolute))
            {
                System.IO.File.WriteAllText($"/home/{GetSessionUser().Username}/files/{cvm.EnvironmentName}/dl.txt", cvm.File);

                DB.CmdAction.CreateTask(new CmdAction
                {
                    Action = "download_extract",
                    Id_Variable = lastID,
                    ExecutedById = GetSessionUser().ID
                });
            }

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
                Value = cvm.SWVersion
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

            return RedirectToAction("Index", "Home");
        }

        public ActionResult Delete(long id)
        {
            var env = DB.Environments.Get(id);
            if (env == null)
            {
                AddError("Could not find Environment");
                return RedirectToAction("Index", "Home");
            }
            return View(env);
        }

        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            var env = DB.Environments.Get(id);
            if (env == null)
            {
                AddError("Could not find Environment");
                return RedirectToAction("Index", "Home");
            }

            await DB.Environments.DeleteAsync(env, GetSessionUser()).ConfigureAwait(false);
            AddInfo("Environment sucessfull deleted");
            return RedirectToAction("Index", "Home");
        }
    }
}
