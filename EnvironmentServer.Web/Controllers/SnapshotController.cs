using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    public class SnapshotController : ControllerBase
    {
        public SnapshotController(Database database) : base(database) { }

        public IActionResult Index(long id)
        {
            return View(DB.Snapshot.GetForEnvironment(id));
        }

        [HttpGet]
        public IActionResult Create(long id)
        {
            return View(new EnvironmentSnapshot { EnvironmentId = id });
        }

        [HttpPost]
        public IActionResult Create([FromForm] EnvironmentSnapshot env_snap, long id)
        {
            DB.Snapshot.CreateSnapshot(env_snap.Name, id, GetSessionUser().ID);
            DB.Environments.SetTaskRunning(id, true);
            AddInfo("Environment Snapshot will be createt in a few seconds");
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
            DB.Environments.SetTaskRunning(id, true);
            AddInfo("Environment Snapshot will be restored, this can take a few seconds");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult RestoreConfirm(long id)
        {
            return View(DB.Snapshot.Get(id));
        }

        public IActionResult Delete(long id)
        {
            DB.Snapshot.DeleteSnapshot(id);
            AddInfo("Snapshot deleted!");
            return RedirectToAction("Index");
        }

    }
}