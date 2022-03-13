using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace EnvironmentServer.Web.Controllers
{
    [AllowNotLoggedIn]
    public class RecoverController : ControllerBase
    {
        private Database DB;

        public RecoverController(Database db)
        {
            DB = db;
        }

        [Route("{id}")]
        public IActionResult StartRecover(long id)
        {
            DB.Environments.Use(id);
            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "restore_environment",
                Id_Variable = id,
                ExecutedById = 0
            });
            return RedirectToAction(nameof(WaitForRecover), id);
        }

        public IActionResult WaitForRecover(long id)
        {
            var env = DB.Environments.Get(id);
            if (!env.Stored)
                return Redirect("https://" + env.Address);

            return View();
        }
    }
}
