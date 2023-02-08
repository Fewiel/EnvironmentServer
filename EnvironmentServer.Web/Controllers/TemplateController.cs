using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Attributes;
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
        public TemplateController(Database db) : base(db) { }

        [Permission("template_manage")]
        public IActionResult Index()
        {
            return View(DB.Templates.GetAllSorted());
        }

        [Permission("template_manage")]
        public IActionResult Delete(long id)
        {
            DB.Templates.StartDelete(id);

            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "delete_template",
                Id_Variable = id,
                ExecutedById = GetSessionUser().ID
            });

            return RedirectToAction("Index");
        }

        [Permission("templates_create")]
        public IActionResult Create(long id)
        {
            return View(new CreateTemplateViewModel { EnvironmentID = id});
        }

        [HttpPost]
        [Permission("templates_create")]
        public IActionResult Create([FromForm] CreateTemplateViewModel ctvm)
        {
            if (string.IsNullOrEmpty(ctvm.Name))
            {
                AddError("Name is required!");
                return View(ctvm);
            }

            if (string.IsNullOrEmpty(ctvm.Descirption))
            {
                AddError("Descirption is required!");
                return View(ctvm);
            }

            var env = DB.Environments.Get(ctvm.EnvironmentID);

            ctvm.Name = ctvm.Name.ToLower().Replace(" ", "_");

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
