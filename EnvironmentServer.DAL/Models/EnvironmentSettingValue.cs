using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Models
{
    public class EnvironmentSettingValue
    {
        public long EnvironmentID { get; set; }
        public long EnvironmentSettingID { get; set; }
        public EnvironmentSetting EnvironmentSetting { get; set; }
        public string Value { get; set; }
    }
}
