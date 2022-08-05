using EnvironmentServer.DAL.Interfaces;
using System;

namespace EnvironmentServer.DAL.Models;

public class DockerComposeFile : IDBIdentifier
{
    public long ID { get; set; }
    public long UserID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Content { get; set; }
    public DateTimeOffset Created { get; set; }
}