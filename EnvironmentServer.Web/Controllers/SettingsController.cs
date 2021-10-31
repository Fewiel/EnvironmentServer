using EnvironmentServer.DAL;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Attributes;

namespace EnvironmentServer.Web.Controllers
{
    [AdminOnly]
    public class SettingsController : ControllerBase
    {
        private Database DB;

        public SettingsController(Database database)
        {
            DB = database;
        }

        public IActionResult Index()
        {
            return View(GetNewAndAll());
        }

        private IEnumerable<Setting> GetNewAndAll()
        {
            yield return new Setting();

            foreach (var s in DB.Settings.GetAll())
                yield return s;
        }

        [HttpPost]
        public IActionResult Create([FromForm]Setting setting)
        {
            DB.Settings.Insert(setting);
            AddInfo("Setting " + setting.Key + " added!");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Save([FromForm] Setting setting)
        {
            DB.Settings.Update(setting);
            AddInfo("Setting " + setting.Key + " saved!");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete([FromForm] Setting setting)
        {
            DB.Settings.Delete(setting);
            AddInfo("Setting " + setting.Key + " removed!");
            return RedirectToAction("Index");
        }
    }
}
