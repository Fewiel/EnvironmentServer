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

        public async Task<IActionResult> StartElasticSearchAsync(long id, [FromForm] UpdateViewModel cvm)
        {

            if (DB.EnvironmentsES.GetByEnvironmentID(id) == null)
            {
                await DB.EnvironmentsES.AddAsync(id, cvm.ElasticSearch.ESVersion);
            }
            else if (DB.EnvironmentsES.GetByEnvironmentID(id).ESVersion != cvm.ElasticSearch.ESVersion)
            {
                await DB.EnvironmentsES.Remove(cvm.ElasticSearch.DockerID);
                await DB.EnvironmentsES.AddAsync(id, cvm.ElasticSearch.ESVersion);
            }
            else if (DB.EnvironmentsES.GetByEnvironmentID(id).Active == false)
            {
                await DB.EnvironmentsES.StartContainer(cvm.ElasticSearch.DockerID);
            }
            else
            {
                await DB.EnvironmentsES.StopContainer(cvm.ElasticSearch.DockerID);
            }

            var createViewModel = new UpdateViewModel()
            {
                ID = id,
                EnvironmentName = DB.Environments.Get(id).Name,
                PhpVersions = System.Enum.GetValues(typeof(PhpVersion)).Cast<PhpVersion>()
                    .Select(v => new SelectListItem(v.AsString(), ((int)v).ToString())),
                ElasticSearch = DB.EnvironmentsES.GetByEnvironmentID(id)
            };
            return View("Update", createViewModel);
        }

        [HttpGet]
        public IActionResult Update(long id)
        {
            var es = DB.EnvironmentsES.GetByEnvironmentID(id);
            if (es == null)
            {
                es = new EnvironmentES
                {
                    Active = false,
                    DockerID = "Not configured",
                    EnvironmentID = id,
                    ESVersion = "",
                    Port = 0
                };
            }

            var createViewModel = new UpdateViewModel()
            {
                ID = id,
                EnvironmentName = DB.Environments.Get(id).Name,
                PhpVersions = System.Enum.GetValues(typeof(PhpVersion)).Cast<PhpVersion>()
                    .Select(v => new SelectListItem(v.AsString(), ((int)v).ToString())),
                ElasticSearch = es
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

            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "delete_environment",
                Id_Variable = env.ID,
                ExecutedById = GetSessionUser().ID
            });

            DB.Environments.SetTaskRunning(id, true);

            AddInfo("Delete in progress...");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Sorting() => View(DB.Environments.GetForUser(GetSessionUser().ID));

        public IActionResult Increase(long id)
        {
            DB.Environments.IncreaseSorting(id);
            return RedirectToAction("Sorting", "Env");
        }

        public IActionResult Decrease(long id)
        {
            DB.Environments.DecreaseSorting(id);
            return RedirectToAction("Sorting", "Env");
        }
    }
}
