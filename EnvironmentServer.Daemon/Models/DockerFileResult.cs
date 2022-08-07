using System.Collections.Generic;

namespace EnvironmentServer.Daemon.Models;
public class DockerFileResult
{
    public string Content { get; set; }
    public Dictionary<string, string> Variables { get; set; } = new();
    public List<int> Ports { get; set; } = new();

    public void AddPort(string variable, int port)
    {
        Variables.Add(variable, port.ToString());
        Ports.Add(port);
    }
}