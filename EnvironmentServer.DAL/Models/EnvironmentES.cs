namespace EnvironmentServer.DAL.Models;

public class EnvironmentES
{
    public long ID { get; set; }
    public long EnvironmentID { get; set; }
    public string ESVersion { get; set; }
    public int Port { get; set; }
    public string DockerID { get; set; }
    public bool Active { get; set; }
}