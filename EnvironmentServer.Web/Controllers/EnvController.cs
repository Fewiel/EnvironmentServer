using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.ViewModels.Env;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace EnvironmentServer.Web.Controllers
{
    public class EnvController : ControllerBase
    {
        public EnvController(Database database) : base(database) { }

        public IActionResult Index()
        {
            return View();
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
            var createViewModel = new UpdateViewModel()
            {
                ID = id,
                DisplayName = DB.Environments.Get(id).DisplayName,
                EnvironmentName = DB.Environments.Get(id).InternalName,
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
                AddError("Environment not found");
                return RedirectToAction("Index", "Home");
            }

            if (env.UserID != GetSessionUser().ID)
                return RedirectToAction("Index", "Home");

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
                    AddError($"You have reached the maximun number of permanent Environments! You can only have {limit} permanent Environment(s)!");
                    return RedirectToAction("Index", "Home");
                }
            }

            DB.Environments.ChangePermanent(id);
            AddInfo("Environment is set to permanent");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ConfigFileAsync(long id)
        {
            var swConfig = await DB.ShopwareConfig.GetByEnvIDAsync(id);

            if (swConfig == null)
            {
                swConfig = new ShopwareConfig();
                swConfig.ID = -1;
                swConfig.EnvID = id;

                DB.Environments.SetTaskRunning(swConfig.EnvID, true);

                DB.CmdAction.CreateTask(new CmdAction
                {
                    Action = "get_config",
                    Id_Variable = swConfig.EnvID,
                    ExecutedById = GetSessionUser().ID
                });
            }

            return View(swConfig);
        }

        [HttpPost]
        public async Task<IActionResult> ConfigFileAsync(ShopwareConfig swConfig)
        {
            await DB.ShopwareConfig.UpdateAsync(swConfig);

            DB.Logs.Add("Web", JsonConvert.SerializeObject(swConfig));

            DB.Environments.SetTaskRunning(swConfig.EnvID, true);

            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "write_config",
                Id_Variable = swConfig.EnvID,
                ExecutedById = GetSessionUser().ID
            });

            AddInfo("Config Saved");
            return View(swConfig);
        }

        public IActionResult UpdateConfigFile(long id)
        {
            DB.Environments.SetTaskRunning(id, true);

            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "update_config",
                Id_Variable = id,
                ExecutedById = GetSessionUser().ID
            });
            AddInfo("Update triggered - Wait until the task has been executed"); 
            return RedirectToAction("Index", "Home");
        }
    }
}