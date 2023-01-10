using System;

namespace EnvironmentServer.Daemon.Models;

public class EnvironmentBackup
{
    public string Name { get; set; }
    public string Username { get; set; }
    public DateTime BackupDate { get; set; }
}