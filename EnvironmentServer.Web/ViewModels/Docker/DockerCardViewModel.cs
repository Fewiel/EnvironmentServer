using EnvironmentServer.Web.Models;

namespace EnvironmentServer.Web.ViewModels.Docker;

public class DockerCardViewModel
{
    public DockerContainerData Data { get; set; }
    public string Domain { get; set; }
}