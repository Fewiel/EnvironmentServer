using System;

namespace EnvironmentServer.DAL.Models;

public class DockerContainer
{
    public long ID { get; set; }
    public string DockerID { get; set; }
    public long UserID { get; set; }
    public string Name { get; set; }
    public long DockerComposeFileID { get; set; }
    public bool Active { get; set; }
    public DateTimeOffset LatestUse { get; set; }
    public DateTimeOffset Created { get; set; }
}