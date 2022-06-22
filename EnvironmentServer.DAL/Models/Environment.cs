using EnvironmentServer.DAL.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Models
{
    public class Environment
    {
        public long ID { get; set; }
        [Description("users_ID_fk")]
        public long UserID { get; set; }
        public string DisplayName { get; set; }
        public string InternalName { get; set; }
        public string Address { get; set; }
        public string DBPassword { get; set; }
        public PhpVersion Version { get; set; }
        public List<EnvironmentSettingValue> Settings { get; set; }
        public int Sorting { get; set; }
        public DateTime LatestUse { get; set; }
        public bool Stored { get; set; }
        public bool DevelopmentMode { get; set; }
        public bool Permanent { get; set; }
    }
}
