﻿using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Repositories;
using EnvironmentServer.Web.Attributes;
using EnvironmentServer.Web.Extensions;
using EnvironmentServer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    public class ControllerBase : Controller
    {
        protected readonly Database DB;

        public ControllerBase(Database db)
        {
            DB = db;
        }

        private void AddMessageInternal(Message msg)
        {
            var msgs = TempData.Get<List<Message>>("messages") ?? new List<Message>();
            msgs.Add(msg);
            TempData.Put("messages", msgs);
        }

        protected void AddError(string msg) => AddMessageInternal(new Message(msg, "alert-danger"));
        protected void AddInfo(string msg) => AddMessageInternal(new Message(msg, "alert-primary"));

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var attributes = new List<object>();

            //get attributes of action
            if (context.ActionDescriptor is ControllerActionDescriptor cad)
                attributes.AddRange(cad.MethodInfo.GetCustomAttributes(true));

            //get attributes of controller
            attributes.AddRange(Attribute.GetCustomAttributes(context.Controller.GetType(), true));

            var allowNotLoggedIn = false;

            foreach (var attr in attributes)
            {
                if (attr is AdminOnlyAttribute && !IsAdmin())
                {
                    AddError("You are not permitted to view this page.");
                    SetRedirect(context);
                    return;
                }

                if (attr is PermissionAttribute pa && !HasPermission(pa.PermissionName))
                {
                    AddError("You are not permitted to view this page.");
                    SetRedirect(context);
                    return;
                }

                if (attr is AllowNotLoggedInAttribute)
                    allowNotLoggedIn = true;
            }

            if (!allowNotLoggedIn && !IsLoggedIn())
            {
                context.Result = new RedirectToRouteResult("login", new { controller = "Login" });
                return;
            }

            base.OnActionExecuting(context);
        }

        public bool IsLoggedIn() => HttpContext.Session.TryGetValue("user", out _);

        public bool IsAdmin()
        {
            var usr = HttpContext.Session.GetObject<User>("user");
            if (usr == null)
                return false;
            return usr.IsAdmin;
        }

        private bool HasPermission(string permissionName)
        {
            var usr = HttpContext.Session.GetObject<User>("user");
            if (usr == null)
                return false;

            return DB.Permission.HasPermission(usr, permissionName);
        }

        private void SetRedirect(ActionExecutingContext context)
        {
            if (IsLoggedIn())
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
            else
            {
                context.Result = new RedirectToRouteResult("login", new { controller = "Login" });
            }
        }

        public User GetSessionUser() => HttpContext.Session.GetObject<User>("user");
    }
}
