using Dapper;
using EnvironmentServer.DAL.Repositories;
using EnvironmentServer.DAL.Utility;
using MySql.Data.MySqlClient;

namespace EnvironmentServer.DAL.Migrations;

internal class EnvironmentDBPasswordMigration
{
    public static void Migrate(Database db)
    {
        using var c = new MySQLConnectionWrapper(db.ConnString);

        foreach (var env in db.Environments.GetAll())
        {
            if (!string.IsNullOrEmpty(env.DBPassword))
                continue;

            var dbPassword = UsersRepository.RandomPasswordString(16);
            var dbString = db.Users.GetByID(env.UserID).Username + "_" + env.InternalName;

            c.Connection.Execute("UPDATE `environments` SET `DBPassword` = @password WHERE `environments`.`ID` = @id;", new
            {
                id = env.ID,
                password = dbPassword
            });

            c.Connection.Execute($"create user {MySqlHelper.EscapeString(dbString)}@'localhost' identified by @password;", new
            {
                password = dbPassword
            });

            c.Connection.Execute($"grant all on {MySqlHelper.EscapeString(dbString)}.* to '{MySqlHelper.EscapeString(dbString)}'@'localhost';");

            c.Connection.Execute("UPDATE mysql.user SET Super_Priv='Y';");
            c.Connection.Execute("FLUSH PRIVILEGES;");
        }
    }
}
