using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.ViewModels.Templates;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

namespace EnvironmentServer.Web.Controllers
{

    //var tmpDetails = new TemplateDetails { EnvironmentID = lastID, TemplateID = esv.TemplateID };

    //var detailsID = DB.CmdActionDetail.Create(JsonConvert.SerializeObject(tmpDetails));

    public class TemplateController : ControllerBase
    {
        private readonly Database DB;

        public TemplateController(Database db)
        {
            DB = db;
        }

        public IActionResult Create(long id)
        {
            return View(new CreateTemplateViewModel { EnvironmentID = id});
        }

        [HttpPost]
        public IActionResult Create([FromForm] CreateTemplateViewModel ctvm)
        {
            var env = DB.Environments.Get(ctvm.EnvironmentID);

            var tpl = new Template
            {
                Name = ctvm.Name,
                Description = ctvm.Descirption,
                FastDeploy = false,
                UserID = GetSessionUser().ID,
                ShopwareVersion = env.Settings.Find(s => s.EnvironmentSettingID == 3).Value
            };

            var lastID = DB.Templates.Create(tpl);
            var tmpDetails = new TemplateDetails { EnvironmentID = env.ID, TemplateID = lastID };
            var detailsID = DB.CmdActionDetail.Create(JsonConvert.SerializeObject(tmpDetails));

            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "create_template",
                Id_Variable = detailsID,
                ExecutedById = GetSessionUser().ID
            });

            DB.Environments.SetTaskRunning(env.ID, true);
            return RedirectToAction("Index", "Home");
        }
    }
}
