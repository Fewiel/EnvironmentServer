using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Models
{
    public class Php
    {
        //https://blog.frehi.be/2019/02/16/running-different-php-applications-as-different-users/

        public string Version { get; set; }
        public string Path { get; set; }
        public ShellUser User { get; set; }
    }
}
