using System;

namespace EnvironmentServer.DAL.Models
{
    public class DockerInstance
    {
        public long ID { get; set; }
        public string InstanceID { get; set; }
        public string Name { get; set; }
        public int Port { get; set; }
        public int EnvironmentID { get; set; }
        public string Image { get; set; }
        public bool Interactive { get; set; }
        public string[] DockerEnvironment { get; set; }
        public string[] PortMappings { get; set; }
        public bool Running { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastChange { get; set; }
    }
}