using EnvironmentServer.DAL.Enums;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Models
{
    public class ScheduleAction
    {
        public long Id { get; set; }
        public string Action { get; set; }
        public Timing Timing { get; set; }
        public int Interval { get; set; }
        public DateTime LastExecuted { get; set; }
    }
}
