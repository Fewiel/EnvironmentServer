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
        public EnvController(Database database) : base(database) { }

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
                EnvironmentName = DB.Environments.Get(id).InternalName,
                PhpVersions = System.Enum.GetValues(typeof(PhpVersion)).Cast<PhpVersion>()
                    .Select(v => new SelectListItem(v.AsString(), ((int)v).ToString())),
                ElasticSearch = DB.EnvironmentsES.GetByEnvironmentID(id)
            };
            return View("Update", createViewModel);
        }

        public IActionResult Development(long id)
        {
            DB.Environments.SetTaskRunning(id, true);

            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "environment_set_dev",
                Id_Variable = id,
                ExecutedById = GetSessionUser().ID
            });

            AddInfo("Updateting Development Mode");
            return RedirectToAction("Index", "Home");
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
                DisplayName = DB.Environments.Get(id).DisplayName,
                EnvironmentName = DB.Environments.Get(id).InternalName,
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

        [HttpPost]
        public IActionResult Rename(long id, [FromForm] UpdateViewModel cvm)
        {
            DB.Environments.SetDisplayName(id, cvm.DisplayName);
            AddInfo("Environment Name Changed");
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

        public IActionResult OpenFrontend(long id)
        {
            var env = DB.Environments.Get(id);
            DB.Environments.Use(id);
            return Redirect("https://" + env.Address);
        }

        public IActionResult OpenBackend(long id)
        {
            var env = DB.Environments.Get(id);
            var version = env.Settings.Find(s => s.EnvironmentSetting.Property == "sw_version").Value;
            DB.Environments.Use(id);
            if (env.Stored)
                return Redirect("https://" + env.Address);
            return Redirect("https://" + env.Address + (version[0] == '5' ? "/backend" : "/admin"));
        }

        public IActionResult ChangePerma(long id)
        {
            var permEnvironments = DB.Environments.GetPermanentForUser(GetSessionUser().ID);
            var usr = DB.Users.GetByID(GetSessionUser().ID);
            var env = DB.Environments.Get(id);

            if (permEnvironments != null)
            {
                var limit = DB.Limit.GetLimit(usr, "environments_max_perm");
                if (limit <= permEnvironments.Count() && !env.Permanent)
                {
                    AddError($"You have reached the maximun number of permanent Environments! You can have {limit} permanent Environments!");
                    return RedirectToAction("Index", "Home");
                }
            }

            DB.Environments.ChangePermanent(id);
            AddInfo("Environment is set to permanent");
            return RedirectToAction("Index", "Home");
        }
    }
}
