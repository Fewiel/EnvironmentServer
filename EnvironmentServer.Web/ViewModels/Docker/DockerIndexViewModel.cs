using EnvironmentServer.DAL.Models;
using System.Collections.Generic;

namespace EnvironmentServer.Web.ViewModels.Docker;

public class DockerIndexViewModel
{
    public IEnumerable<DockerContainer> Containers { get; set; }
}