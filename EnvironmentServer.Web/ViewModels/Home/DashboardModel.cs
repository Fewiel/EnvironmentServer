using EnvironmentServer.DAL.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.ViewModels.Home
{
    public class DashboardModel
    {
        public Dictionary<string, string> PerformanceData { get; set; }
        public int UserCount { get; set; }
        public int EnvironmentCount { get; set; }
        public int StoredCount { get; set; }
        public int Queue { get; set; }
        public IEnumerable<Environment> Environments { get; set; }
        public string Htaccess { get; set; }
    }
}
