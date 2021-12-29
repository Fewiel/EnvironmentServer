using System;

namespace EnvironmentServer.DAL.Models;

public class News
{
    public long ID { get; set; }
    public long UserID { get; set; }
    public string Content { get; set; }
    public DateTime Created { get; set; }
}
