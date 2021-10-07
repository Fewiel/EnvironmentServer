using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    public class SnapshotController : ControllerBase
    {
        private Database DB;

        public SnapshotController(Database database)
        {
            DB = database;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create(long ID)
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create([FromForm] EnvironmentSnapshot env_snap, long ID)
        {
            DB.Snapshot.CreateSnapshot(env_snap.Name, ID, GetSessionUser().ID);
            AddInfo("Environment Snapshot will be createt in a few seconds");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Restore()
        {
            return View();
        }

        public IActionResult Delete()
        {
            return View();
        }
    }
}