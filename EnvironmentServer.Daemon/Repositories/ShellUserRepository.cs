using EnvironmentServer.Daemon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Repositories
{
    public class ShellUserRepository
    {
        //https://aventistech.com/kb/configure-sftp-server-in-debian/


        public void Create(ShellUser user)
        {
            Cmd.Run("useradd -p $(openssl passwd -1 " + user.Password + ") " + user.Username);
            Cmd.Run("usermod -G sftp_users " + user.Username);
            Cmd.Run("mkdir /home/" + user.Username);
            Cmd.Run("sudo chown " + user.Username + ":sftp_users /SFTP/" + user.Username);
        }

        public void Delete(ShellUser user)
        {
            Cmd.Run("userdel -r " + user.Username);
            Cmd.Run("rm -R /home/" + user.Username);
            //MySQL Datenbanken entfernen
        }

    }
}
