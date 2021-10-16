using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
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

        public IActionResult Index(long id)
        {
            return View(DB.Snapshot.GetForEnvironment(id));
        }

        [HttpGet]
        public IActionResult Create(long ID)
        {
            return View(new EnvironmentSnapshot { EnvironmentId = ID });
        }

        [HttpPost]
        public IActionResult Create([FromForm] EnvironmentSnapshot env_snap, long ID)
        {
            DB.Snapshot.CreateSnapshot(env_snap.Name, ID, GetSessionUser().ID);
            AddInfo("Environment Snapshot will be createt in a few seconds");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult RestoreLatest(long id)
        {
            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "snapshot_restore_latest",
                Id_Variable = id,
                ExecutedById = GetSessionUser().ID
            });
            return RedirectToAction("Index", "Home");
        }

        public IActionResult RestoreConfirmed(long id)
        {
            DB.CmdAction.CreateTask(new CmdAction
            {
                Action = "snapshot_restore",
                Id_Variable = id,
                ExecutedById = GetSessionUser().ID
            });
            AddInfo("Environment Snapshot will be restored, this can take a few seconds");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult RestoreConfirm(long id)
        {
            return View(DB.Snapshot.Get(id));
        }
    }
}