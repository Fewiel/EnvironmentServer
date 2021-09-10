using EnvironmentServer.Web.ViewModels.Login;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    public class LoginController : Controller
    {
        [Route("/Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login([FromForm]LoginViewModel lvm)
        {
            if (!ModelState.IsValid)
                return View();

            return View();
        }
    }
}
