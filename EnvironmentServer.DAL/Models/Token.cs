using System;

namespace EnvironmentServer.DAL.Models;

public class Token
{
    public long ID { get; set; }
    public string Guid { get; set; }
    public long UserID { get; set; }
    public bool Used { get; set; }
    public DateTime Created { get; set; }
}
