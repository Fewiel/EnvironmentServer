using EnvironmentServer.DAL.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Models
{
    public class Environment
    {
        public long ID { get; set; }
        public long UserID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public PhpVersion Version { get; set; }
    }
}
