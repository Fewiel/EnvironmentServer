﻿using CliWrap;

namespace EnvironmentServer.Utility;

public static class Bash
{
    public static Action<string, string>? LogCallback { get; set; }

    public static async Task CommandAsync(string cmd, string? workingDir = null, bool log = true)
    {
        if (log)
            LogCallback?.Invoke("Bash Command", cmd);

        if (workingDir == null)
        {
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"{cmd}\"")
                .ExecuteAsync();
        }
        else
        {
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"{cmd}\"")
                .WithWorkingDirectory(workingDir)
                .ExecuteAsync();
        }
    }

    public static async Task ApacheEnableSiteAsync(string siteConfig) => await CommandAsync($"a2ensite {siteConfig}");

    public static async Task ApacheDisableSiteAsync(string siteConfig) => await CommandAsync($"a2dissite {siteConfig}");

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