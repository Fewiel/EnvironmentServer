using EnvironmentServer.DAL.Interfaces;

namespace EnvironmentServer.DAL.Models;
public class DockerPort
{
    public int Port { get; set; }
    public string Name { get; set; }
    public long DockerContainerID { get; set; }
}