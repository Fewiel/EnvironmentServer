using CliWrap;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace EnvironmentServer.Util;

public static class Bash
{
    public static Action<string, string>? LogCallback { get; set; }

    public static async Task CommandAsync(string cmd, string? workingDir = null, bool log = true, bool validation = true)
    {
        if (log)
            LogCallback?.Invoke("Bash Command", $"{cmd}");

        if (log && workingDir != null)
            LogCallback?.Invoke("Bash Command", $"Working Dir: {workingDir}");

        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{cmd}\"",
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        proc.Start();

        var result = proc.StandardOutput.ReadToEnd();
        var error = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (validation && proc.ExitCode != 0)
            LogCallback?.Invoke("ERROR Bash", $"Bash Process Failed with exit code: {proc.ExitCode}; Standard Error:{Environment.NewLine}{error}{Environment.NewLine}{result}");

        if (log)
            LogCallback?.Invoke("Bash Command OutoutPipe", $"Result: {JsonSerializer.Serialize(result)}");
        if (log)
            LogCallback?.Invoke("Bash Command ErrorPipe", $"Result: {JsonSerializer.Serialize(error)}");
        if (log)
            LogCallback?.Invoke("Bash Command", $"Finished {cmd}");
    }

    public static async Task<StringBuilder> CommandQueryAsync(string cmd, string workingDir)
    {
        StringBuilder result = new();
        await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"{cmd}\"")
                .WithWorkingDirectory(workingDir)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(result))
                .ExecuteAsync();
        return result;
    }

    public static async Task ApacheEnableSiteAsync(string siteConfig) => await CommandAsync($"a2ensite {siteConfig}", validation: false);

    public static async Task ApacheDisableSiteAsync(string siteConfig) => await CommandAsync($"a2dissite {siteConfig}", validation: false);

    public static async Task ReloadApacheAsync() => await CommandAsync("service apache2 reload");

    public static async Task ChmodAsync(string permission, string path, bool recrusiv = false)
        => await CommandAsync($"chmod{(recrusiv ? " -R" : "")} {permission} {path}");

    public static async Task ChownAsync(string user, string group, string path, bool recrusiv = false)
        => await CommandAsync($"chown{(recrusiv ? " -R" : "")} {user}:{group} {path}");

    public static async Task UserAddAsync(string user, string password)
        => await CommandAsync($"useradd -p $(openssl passwd -1 $'{password}') {user}", log: false);

    public static async Task UserModGroupAsync(string user, string group)
        => await CommandAsync($"usermod -G {group} {user}");

    public static async Task ServiceReloadAsync(string service) => await CommandAsync($"service {service} reload");
}