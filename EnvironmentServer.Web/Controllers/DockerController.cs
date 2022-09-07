using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    public class DockerController : ControllerBase
    {
        public DockerController(Database db) : base(db)
        {
        }

        public async Task<IActionResult> IndexAsync()
        {
            List<DockerContainer> containers = (List<DockerContainer>)await DB.DockerContainer.GetAllForUserAsync(GetSessionUser().ID);
            List<DockerContainerData> dData = new();

            foreach (var c in containers)
            {
                dData.Add(new() { Container = c, ContainerPorts = DB.DockerPort.GetForContainer(c.ID) });
            }

            return View();
        }
    }
}
