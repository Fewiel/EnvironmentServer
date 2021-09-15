using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Models
{
    public class ShellUser
    {
        public string Username { get; set; }
        public String Password { get; set; }
        public string HomeDir { get; set; }
    }
}
