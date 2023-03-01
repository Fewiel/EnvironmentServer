using EnvironmentServer.DAL;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Attributes;
using EnvironmentServer.Util;

namespace EnvironmentServer.Web.Controllers
{
    [Permission("settings_edit")]
    public class SettingsController : ControllerBase
    {
        public SettingsController(Database database) : base(database) { }

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
        public IActionResult Create([FromForm] Setting setting)
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

        public async Task<IActionResult> RestartPhpAsync()
        {
            await Bash.ServiceReloadAsync("php5.6-fpm");
            await Bash.ServiceReloadAsync("php7.2-fpm");
            await Bash.ServiceReloadAsync("php7.4-fpm");
            await Bash.ServiceReloadAsync("php8.0-fpm");
            await Bash.ServiceReloadAsync("php8.1-fpm");
            await Bash.ReloadApacheAsync();
            return RedirectToAction("Index");
        }
    }
}
