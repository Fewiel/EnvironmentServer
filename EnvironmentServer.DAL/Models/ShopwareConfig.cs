using EnvironmentServer.DAL.Interfaces;
using System;

namespace EnvironmentServer.DAL.Models;

public class ShopwareConfig : IDBIdentifier
{
    public long ID { get; set; }
    public long EnvID { get; set; }
    public string Content { get; set; }
    public DateTimeOffset LatestChange { get; set; }
}