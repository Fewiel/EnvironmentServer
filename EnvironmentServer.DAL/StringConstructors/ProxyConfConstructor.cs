namespace EnvironmentServer.DAL.StringConstructors;

public class ProxyConfConstructor
{
    private string Domain;
    private string Port;

    public static ProxyConfConstructor Construct => new();

    public ProxyConfConstructor WithDomain(string domain)
    {
        Domain = domain;
        return this;
    }

    public ProxyConfConstructor WithPort(int port)
    {
        Port = Port.ToString();
        return this;
    }

    public string BuildHttpProxy()
    {
        return $@"
<VirtualHost *:80>
    ServerName {Domain}
    Redirect permanent / {Domain}
</VirtualHost>

<VirtualHost *:443>
    LoadModule ssl_module /usr/lib64/apache2-prefork/mod_ssl.so
    ServerName {Domain}
    ServerAlias {Domain}
    ProxyPreserveHost On
    ProxyPass / http://127.0.0.1:{Port}/
    ProxyPassReverse / http://127.0.0.1:{Port}/
    SSLEngine on
    SSLCertificateFile /etc/letsencrypt/live/shopdev.de/cert.pem
    SSLCertificateKeyFile /etc/letsencrypt/live/shopdev.de/privkey.pem
    SSLCertificateChainFile /etc/letsencrypt/live/shopdev.de/fullchain.pem
</VirtualHost>
";
    }
}