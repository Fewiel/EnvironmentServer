﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Repositories;
using EnvironmentServer.DAL.Utility;
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
        public EnvironmentESRepository EnvironmentsES { get; }
        public UserInformationRepository UserInformation { get; }
        public DepartmentRepository Department { get; }
        public ExhibitionVersionRepository ExhibitionVersion { get; }
        public PerformanceRepository Performance { get; }
        public TemplateRepository Templates { get; }
        public CmdActionDetailsRepository CmdActionDetail { get; }

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
            EnvironmentsES = new EnvironmentESRepository(this);
            UserInformation = new UserInformationRepository(this);
            Department = new DepartmentRepository(this);
            ExhibitionVersion = new ExhibitionVersionRepository(this);
            Performance = new PerformanceRepository(this);
            Templates = new TemplateRepository(this);
            CmdActionDetail = new CmdActionDetailsRepository(this);

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
            using var c = new MySQLConnectionWrapper(ConnString);
            return c.Connection;
        }
    }
}
