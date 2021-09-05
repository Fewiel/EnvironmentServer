using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Models
{
    public class Setting
    {
        public long ID { get; set; }
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public string Value { get; set; }
    }
}
