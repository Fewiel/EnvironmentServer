using EnvironmentServer.Daemon.Utility;
using EnvironmentServer.DAL;
using EnvironmentServer.Interfaces;
using EnvironmentServer.Util;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using EnvironmentServer.Daemon.Models;

namespace EnvironmentServer.Daemon.Actions;

public class BackupEnvironment : ActionBase
{
    public override string ActionIdentifier => "backup_environment";

    public override async Task ExecuteAsync(ServiceProvider sp, long variableID, long userID)
    {
        var db = sp.GetService<Database>();
        var em = sp.GetService<IExternalMessaging>();

        var env = db.Environments.Get(variableID);
        var usr = db.Users.GetByID(env.UserID);

        var backupDir = $"/home/{usr.Username}/files/backups";
        var dbString = usr.Username + "_" + env.InternalName;

        PackerHelper.DeleteCache(usr.Username, env.InternalName);

        if (Directory.Exists(backupDir))
            Directory.CreateDirectory($"/home/{usr.Username}/files/backups");

        await Bash.CommandAsync($"mysqldump -u {dbString} -p{env.DBPassword} --hex-blob --default-character-set=utf8 " + dbString + " --result-file=db.sql",
            $"/home/{usr.Username}/files/{env.InternalName}");

        var backupInfo = new EnvironmentBackup
        {
            Name = env.InternalName,
            Username = usr.Username,
            BackupDate = DateTime.UtcNow
        };

        File.WriteAllText($"/home/{usr.Username}/files/{env.InternalName}/backup_information.json", System.Text.Json.JsonSerializer.Serialize(backupInfo));

        var backupFile = $"/home/{usr.Username}/files/backups/{env.InternalName}_{DateTime.UtcNow.ToString("yyyy_MM_dd-HH_mm_ss")}.zip";

        await Bash.CommandAsync($"zip -r {backupFile} {env.InternalName}",
            $"/home/{usr.Username}/files/");

        if (!string.IsNullOrEmpty(usr.UserInformation.SlackID))
        {
            var success = await em.SendMessageAsync($"Backup finished for {env.InternalName}. Backup is saved to {backupFile}", usr.UserInformation.SlackID);
            if (success)
                return;
        }
        db.Mail.Send($"Backup finished for {env.InternalName}!", $"Backup finished for {env.InternalName}. Backup is saved to {backupFile}", usr.Email);
    }
} 