﻿using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Attributes;
using EnvironmentServer.Web.Models;
using EnvironmentServer.Web.ViewModels.Rights;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnvironmentServer.Web.Controllers
{
    [Permission("permissions_manage")]
    public class RightsController : ControllerBase
    {
        public RightsController(Database db) : base(db) { }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Roles()
        {
            return View(DB.Role.GetAll());
        }

        public IActionResult AddRole()
        {
            var perm = DB.Permission.GetAll();
            var limits = DB.Limit.GetAll();

            var rvm = new RoleViewModel
            {
                Role = new(),
                Permissions = perm.Select(p => WebPermission.FromPermission(p)).ToList(),
                Limits = limits.Select(limits => WebLimit.FromLimit(limits)).ToList()
            };

            return View(rvm);
        }

        [HttpPost]
        public IActionResult AddRole([FromForm] RoleViewModel rvm)
        {
            Console.WriteLine(JsonConvert.SerializeObject(rvm));

            rvm.Role.ID = DB.Role.Add(new() { Name = rvm.Role.Name, Description = rvm.Role.Description });

            foreach (var p in rvm.Permissions)
            {
                if (p.Enabled)
                    DB.RolePermission.Add(new() { PermissionID = p.ToPermission().ID, RoleID = rvm.Role.ID });
            }

            foreach (var l in rvm.Limits)
            {
                if (l.Value != -1)
                {
                    var limit = l.ToLimit();
                    var rLimit = new RoleLimit()
                    {
                        LimitID = limit.ID,
                        RoleID = rvm.Role.ID,
                        Value = l.Value
                    };

                    DB.RoleLimit.Add(rLimit);
                }
            }

            return RedirectToAction("Roles");
        }

        public IActionResult Update(long id)
        {
            var allPerm = DB.Permission.GetAll();
            var allLimits = DB.Limit.GetAll();

            var perm = DB.RolePermission.GetForRole(id);
            var limits = DB.RoleLimit.GetForRole(id);

            Console.WriteLine(JsonConvert.SerializeObject(limits));

            var webPerm = new List<WebPermission>();
            var webLimits = new List<WebLimit>();

            foreach (var p in allPerm)
            {
                webPerm.Add(new() { Permission = p, Enabled = perm.Any(pe => pe.PermissionID == p.ID) });
            }

            foreach (var l in allLimits)
            {
                webLimits.Add(new() { Limit = l, Value = limits.FirstOrDefault(li => li.LimitID == l.ID)?.Value ?? -1 });
            }

            var rvm = new RoleViewModel
            {
                Role = DB.Role.GetByID(id),
                Permissions = webPerm,
                Limits = webLimits
            };

            return View(rvm);
        }

        [HttpPost]
        public IActionResult Update([FromForm] RoleViewModel rvm)
        {
            DB.Role.ClearLimits(rvm.Role.ID);
            DB.Role.ClearPermissions(rvm.Role.ID);

            foreach (var p in rvm.Permissions)
            {
                if (p.Enabled)
                    DB.RolePermission.Add(new() { PermissionID = p.ToPermission().ID, RoleID = rvm.Role.ID });
            }

            foreach (var l in rvm.Limits)
            {
                if (l.Value != -1)
                {
                    var limit = l.ToLimit();
                    var rLimit = new RoleLimit()
                    {
                        LimitID = limit.ID,
                        RoleID = rvm.Role.ID,
                        Value = l.Value
                    };

                    DB.RoleLimit.Add(rLimit);
                }
            }

            DB.Role.Update(rvm.Role);

            AddInfo("Role updated");
            return RedirectToAction("Roles");
        }

        public IActionResult Delete(long id)
        {
            DB.Role.Delete(id);
            AddInfo("Role updated");
            return RedirectToAction("Roles");
        }
    }
}