using EnvironmentServer.DAL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.ViewModels.Home
{
    public class DashboardModel
    {
        public IEnumerable<Environment> Environments { get; set; }
        public string Htaccess { get; set; }
    }
}
