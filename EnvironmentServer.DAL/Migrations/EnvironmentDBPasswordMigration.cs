using Dapper;
using EnvironmentServer.DAL.Repositories;
using MySql.Data.MySqlClient;

namespace EnvironmentServer.DAL.Migrations;

internal class EnvironmentDBPasswordMigration
{
    public static void Migrate(Database db)
    {
        using (var connection = db.GetConnection())
        {
            foreach (var env in db.Environments.GetAll())
            {
                if (!string.IsNullOrEmpty(env.DBPassword))
                    continue;

                var dbPassword = UsersRepository.RandomPasswordString(16);
                var dbString = db.Users.GetByID(env.UserID).Username + "_" + env.InternalName;

                connection.Execute("UPDATE `environments` SET `DBPassword` = @password WHERE `environments`.`ID` = @id;", new
                {
                    id = env.ID,
                    password = dbPassword
                });

                connection.Execute($"create user {MySqlHelper.EscapeString(dbString)}@'localhost' identified by @password;", new
                {
                    password = dbPassword
                });

                connection.Execute($"grant all on {MySqlHelper.EscapeString(dbString)}.* to '{MySqlHelper.EscapeString(dbString)}'@'localhost';");

                connection.Execute("UPDATE mysql.user SET Super_Priv='Y';");
                connection.Execute("FLUSH PRIVILEGES;");
            }
        }
    }
}
