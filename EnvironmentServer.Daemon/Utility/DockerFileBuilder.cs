using EnvironmentServer.Daemon.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EnvironmentServer.Daemon.Utility;

public static class DockerFileBuilder
{
    private static Regex Regex = new("\\$port:([A-Za-z]*)");

    public static DockerFileResult Build(string template, List<int> usedPorts, int minPort)
    {
        var matches = Regex.Matches(template);
        var result = new DockerFileResult();

        foreach (Match m in matches)
        {
            var port = GetPort(usedPorts, minPort);

            result.AddPort(m.Groups[1].Value, port);

            template = template.Replace(m.Groups[0].Value, port.ToString());
        }

        result.Content = template;
        return result;
    }

    private static int GetPort(List<int> usedPorts, int minPort)
    {
        if (usedPorts.Count == 0)
        {
            usedPorts.Add(minPort);
            return minPort;
        }

        for (int i = 0; i < usedPorts.Count; i++)
        {
            if (i == usedPorts.Count - 1)
            {
                usedPorts.Add(usedPorts[i] + 1);
                return usedPorts[i] + 1;
            }

            if (usedPorts[i] + 1 != usedPorts[i + 1])
            {
                usedPorts.Insert(i + 1, usedPorts[i] + 1);
                return usedPorts[i] + 1;
            }
        }

        throw new InvalidOperationException();
    }
}