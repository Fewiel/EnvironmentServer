using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.ViewModels.Login
{
    public class RegistrationViewModel
    {
        [RegularExpression(@"^[a-z_]([a-z0-9_-]{0,31}|[a-z0-9_-]{0,30}\$)$", ErrorMessage = "Username not allowed")]
        public string Username { get; set; }
    }
}
