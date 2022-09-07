using EnvironmentServer.DAL.Models;
using System.Collections.Generic;

namespace EnvironmentServer.Web.Models
{
    public class DockerContainerData
    {
        public DockerContainer Container { get; set; }
        public IEnumerable<DockerPort> ContainerPorts { get; set; }
    }
}