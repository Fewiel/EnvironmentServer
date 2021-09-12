using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Models
{
    public class Message
    {
        public string Content { get; set; }
        public string Class { get; set; }

        public Message(string content, string cls)
        {
            Content = content;
            Class = cls;
        }
    }
}
