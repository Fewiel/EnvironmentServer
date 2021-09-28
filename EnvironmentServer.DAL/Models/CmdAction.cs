using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Models
{
    public class CmdAction
    {
        public long Id { get; set; }
        public string Action { get; set; }
        public long Id_Variable { get; set; }
        public DateTimeOffset Executed { get; set; }
        public long ExecutedById { get; set; }
    }
}
