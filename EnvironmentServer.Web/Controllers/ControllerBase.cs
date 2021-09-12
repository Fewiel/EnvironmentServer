using EnvironmentServer.Web.Extensions;
using EnvironmentServer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    public class ControllerBase : Controller
    {
        private void AddMessageInternal(Message msg)
        {
            var msgs = TempData.Get<List<Message>>("messages") ?? new List<Message>();
            msgs.Add(msg);
            TempData.Put("messages", msgs);
        }

        protected void AddError(string msg) => AddMessageInternal(new Message(msg, "alert-danger"));
    }
}
