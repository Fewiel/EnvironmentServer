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
                    .Select(v => new SelectListItem(v.AsString(), ((int)v).ToString())),
                Templates = new List<SelectListItem>
                {
                    new SelectListItem("Shopware 6.4.4.1", "1"),
                    new SelectListItem("Shopware 6.4.4.0", "2"),
                    new SelectListItem("Shopware 6.3.3.6", "3"),
                    new SelectListItem("Shopware 5.5.1", "4")
                }
            };
            return View(createViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm] CreateViewModel cvm)
        {
            if (!ModelState.IsValid)
                return View(cvm);

            var environment = new Environment()
            {
                UserID = GetSessionUser().ID,
                Name = cvm.EnvironmentName,
                Address = cvm.EnvironmentName + "." + GetSessionUser().Username + ".shopware.env",
                Version = (PhpVersion)cvm.Version
            };

            var lastID = await DB.Environments.InsertAsync(environment, GetSessionUser()).ConfigureAwait(false);

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
                return RedirectToAction("Index");
            }
            return View(env);
        }

        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            var env = DB.Environments.Get(id);
            if (env == null)
            {
                AddError("Could not find Environment");
                return RedirectToAction("Index");
            }

            await DB.Environments.DeleteAsync(env, GetSessionUser()).ConfigureAwait(false);
            AddInfo("Environment sucessfull deleted");
            return RedirectToAction("Index");
        }
    }
}
