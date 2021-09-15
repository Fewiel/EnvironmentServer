using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon
{
    public class Cmd
    {

        public static void Run(string cmd)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = cmd, };
            Process proc = new Process() { StartInfo = startInfo, };
            proc.Start();
        }

    }
}
