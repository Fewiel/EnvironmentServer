using EnvironmentServer.Web.Models;
using System.Collections.Generic;

namespace EnvironmentServer.Web.ViewModels.Docker;

public class DockerIndexViewModel
{
    public IEnumerable<DockerContainerData> Containers { get; set; }
}