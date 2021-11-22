using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Repositories;
using EnvironmentServer.Web.ViewModels.Env;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;
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

        public IActionResult Delete(long id)
        {
            var env = DB.Environments.Get(id);
            if (env == null)
            {
                AddError("Could not find Environment");
                return RedirectToAction("Index", "Home");
            }

            DB.CmdAction.CreateTask(new CmdAction {
            Action = "delete_environment",
            Id_Variable = env.ID,
            ExecutedById = GetSessionUser().ID
            });

            DB.Environments.SetTaskRunning(id, true);

            AddInfo("Delete in progress...");
            return RedirectToAction("Index", "Home");
        }
    }
}
