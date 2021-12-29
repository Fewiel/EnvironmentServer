using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Repositories;
using EnvironmentServer.Mail;
using MySql.Data.MySqlClient;

namespace EnvironmentServer.DAL
{
    public class Database
    {
        private readonly string ConnString;

        public SettingsRepository Settings { get; }
        public UsersRepository Users { get; }
        public EnvironmentRepository Environments { get; }
        public EnvironmentSettingValueRepository EnvironmentSettings { get; }
        public EnvironmentSnapshotRepository Snapshot { get; }
        public CmdActionRepository CmdAction { get; }
        public LogRepository Logs { get; }
        public Mailer Mail { get; }
        public ScheduleActionRepository ScheduleAction { get; }
        public ShopwareVersionInfoRepository ShopwareVersionInfos { get; }
        public TokenRepository Tokens { get; }
        public NewsRepository News { get; }

        public Database(string connString)
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            var map = new CustomPropertyTypeMap(typeof(Models.Environment), (type, columnName)
            => type.GetProperties().FirstOrDefault(prop => GetDescriptionFromAttribute(prop) == columnName.ToLower()));
            Dapper.SqlMapper.SetTypeMap(typeof(Models.Environment), map);

            ConnString = connString;
            Settings = new SettingsRepository(this);
            Users = new UsersRepository(this);
            Environments = new EnvironmentRepository(this);
            EnvironmentSettings = new EnvironmentSettingValueRepository(this);
            Snapshot = new EnvironmentSnapshotRepository(this);
            CmdAction = new CmdActionRepository(this);
            Logs = new LogRepository(this);
            Mail = new Mailer(this);
            ScheduleAction = new ScheduleActionRepository(this);
            ShopwareVersionInfos = new ShopwareVersionInfoRepository(this);
            Tokens = new TokenRepository(this);
            News = new NewsRepository(this);

            if (Users.GetByUsername("Admin") == null)
            {
                Logs.Add("DAL", "Creating Admin user");
                Task.Run(() => Users.InsertAsync(new User
                {
                    Email = "root@root.tld",
                    Username = "Admin",
                    Password = PasswordHasher.Hash("Admin"),
                    IsAdmin = true
                }, "Admin"));
            }
        }

        static string GetDescriptionFromAttribute(MemberInfo member)
        {
            if (member == null) return null;

            var attrib = (DescriptionAttribute)Attribute.GetCustomAttribute(member, typeof(DescriptionAttribute), false);
            return (attrib?.Description ?? member.Name).ToLower();
        }

        public MySqlConnection GetConnection()
        {
            var c = new MySqlConnection(ConnString);
            c.Open();
            return c;
        }
    }
}
