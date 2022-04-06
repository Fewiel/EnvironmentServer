using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.ScheduleActions
{
    internal class PerformanceCheckCPUMem : ScheduledActionBase
    {
        private const int DigitsInResult = 2;
        private static long totalMemoryInKb;

        public PerformanceCheckCPUMem(ServiceProvider sp) : base(sp)
        {
        }

        public override string ActionIdentifier => "performance_cpu_mem";

        public override async Task ExecuteAsync(Database db)
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = Process.GetProcesses().Sum(a => a.TotalProcessorTime.TotalMilliseconds);
            await Task.Delay(500);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = Process.GetProcesses().Sum(a => a.TotalProcessorTime.TotalMilliseconds);
            var cpuUsedMs = endCpuUsage - startCpuUsage;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            cpuUsageTotal *= 100;

            db.Performance.Set("cpu", cpuUsageTotal.ToString("N" + DigitsInResult));

            var totalMemory = GetTotalMemoryInKb();
            var usedMemory = GetUsedMemoryForAllProcessesInKb();

            db.Performance.Set("memory", ((usedMemory * 100) / totalMemory).ToString("N" + DigitsInResult));
            var diskspace = new DriveInfo("/").AvailableFreeSpace;
            diskspace = diskspace / 1024 / 1024 / 1024;
            db.Performance.Set("diskspace", diskspace.ToString("N" + DigitsInResult));
        }

        private static double GetUsedMemoryForAllProcessesInKb()
        {
            var totalAllocatedMemoryInBytes = Process.GetProcesses().Sum(a => a.PrivateMemorySize64);
            return totalAllocatedMemoryInBytes / 1000;
        }

        private static long GetTotalMemoryInKb()
        {
            const string path = "/proc/meminfo";
            if (!File.Exists(path))
                return 0;

            using var reader = new StreamReader(path);
            string line = string.Empty;
            while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
            {
                if (line.Contains("MemTotal", StringComparison.OrdinalIgnoreCase))
                {
                    // e.g. MemTotal:       16370152 kB
                    var parts = line.Split(':');
                    var valuePart = parts[1].Trim();
                    parts = valuePart.Split(' ');
                    var numberString = parts[0].Trim();

                    var result = long.TryParse(numberString, out totalMemoryInKb);
                    return result ? totalMemoryInKb : 0;
                }
            }
            return 0;
        }
    }
}
