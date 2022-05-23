using System;

namespace EnvironmentServer.DAL.Models;

public class Template
{
    public long ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public long UserID { get; set; }
    public bool FastDeploy { get; set; }
    public string ShopwareVersion { get; set; }
    public DateTimeOffset Created { get; set; }
}