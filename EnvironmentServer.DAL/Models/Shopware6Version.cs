using EnvironmentServer.DAL.Interfaces;
using System;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Models;

public class Shopware6Version
{
    public string Version { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public DateTime Date { get; set; }
    public List<FixedVulnerabilities> FixedVulnerabilities { get; set; }
}

public class FixedVulnerabilities
{
    public string Severity { get; set; }
    public string Summary { get; set; }
    public string Link { get; set; }
}