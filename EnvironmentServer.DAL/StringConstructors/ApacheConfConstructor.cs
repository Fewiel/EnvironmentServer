﻿using EnvironmentServer.DAL.Enums;

namespace EnvironmentServer.DAL.StringConstructors
{
    public class ApacheConfConstructor
    {
        private PhpVersion Version;
        private string Email;
        private string Address;
        private string DocRoot;
        private string LogRoot;
        private string Username;

        public static ApacheConfConstructor Construct => new();

        public ApacheConfConstructor WithVersion(PhpVersion version)
        {
            Version = version;
            return this;
        }

        public ApacheConfConstructor WithEmail(string email)
        {
            Email = email;
            return this;
        }

        public ApacheConfConstructor WithAddress(string address)
        {
            Address = address;
            return this;
        }

        public ApacheConfConstructor WithDocRoot(string docRoot)
        {
            DocRoot = docRoot;
            return this;
        }

        public ApacheConfConstructor WithLogRoot(string logRoot)
        {
            LogRoot = logRoot;
            return this;
        }

        public ApacheConfConstructor WithUsername(string username)
        {
            Username = username;
            return this;
        }

        public string Build()
        {
            return $@"
<VirtualHost *:80>
	<FilesMatch \.php>
        SetHandler ""proxy:unix:/var/run/php/{Version.AsString()}-{Username}.sock|fcgi://localhost/"" 
    </FilesMatch>

	ServerAdmin {Email}
    ServerName {Address}
	DocumentRoot {DocRoot}
    <Directory {DocRoot}>
        Options Indexes FollowSymLinks MultiViews
        AllowOverride All
        Require all granted
    </Directory>

    ErrorLog {LogRoot}/error.log
    CustomLog {LogRoot}/access.log combined

    <IfModule mpm_itk_module>
        AssignUserId {Username} sftp_users
    </IfModule>

<IfModule mod_fastcgi.c>
	AddHandler php-fcgi-handler .php
	Action php-fcgi-handler /php-fcgi-uri
    Alias /php-fcgi-uri fcgi-application
    FastCgiExternalServer fcgi-application -socket /var/run/php/{Version.AsString()}-{Username}.sock -pass-header Authorization -idle-timeout 30000 -flush
</IfModule>

    <IfModule mod_rewrite>
        RewriteEngine On
    </IfModule>
</VirtualHost>";
        }
    }
}