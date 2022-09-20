using EnvironmentServer.DAL.Models;
using System.Collections.Generic;

namespace EnvironmentServer.Web.ViewModels.Docker
{
    public class CreateContainerViewModel
    {
        public DockerContainer Container { get; set; }
        public IEnumerable<DockerComposeFile> ComposeFiles { get; set; }
    }
}
