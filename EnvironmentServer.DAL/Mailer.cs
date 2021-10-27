using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Mail
{
    public class Mailer
    {
        private Database DB;

        public Mailer(Database db)
        {
            DB = db;
        }

        public bool Send(string subject, string body, string recipient)
        {
            if (!bool.TryParse(DB.Settings.Get("smtp_ssl").Value, out var ssl))
            {
                DB.Logs.Add("Mailer", "Error - TryParse smtp_ssl");
                return false;
            }

            if (!int.TryParse(DB.Settings.Get("smtp_port").Value, out var port))
            {
                DB.Logs.Add("Mailer", "Error - TryParse smtp_port");
                return false;
            }

            var smtpClient = new SmtpClient(DB.Settings.Get("smtp_host").Value)
            {
                Port = port,
                Credentials = new NetworkCredential(DB.Settings.Get("smtp_user").Value, DB.Settings.Get("smtp_password").Value),                
                EnableSsl = ssl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(DB.Settings.Get("smtp_mail").Value, "Shopware Environment Server"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };

            mailMessage.To.Add(new MailAddress(recipient));

            smtpClient.Send(mailMessage);

            return true;
        }

    }
}
