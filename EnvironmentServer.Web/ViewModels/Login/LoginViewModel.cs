using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.ViewModels.Login
{
    public class LoginViewModel
    {
        [MinLength(4), MaxLength(64), DataType(DataType.Text)]
        public string Username { get; set; }

        [MinLength(4), DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
