﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Models
{
    public class User
    {
        public long ID { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsAdmin { get; set; }
        public string SSHPublicKey { get; set; }
        public bool Active { get; set; }
        public long RoleID { get; set; }
        public DateTime LastUsed { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public UserInformation UserInformation { get; set; }
        public bool ForcePasswordReset { get; set; }
    }
}
