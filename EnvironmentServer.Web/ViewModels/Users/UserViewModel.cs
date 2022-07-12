using EnvironmentServer.DAL.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace EnvironmentServer.Web.ViewModels.Users
{
    public class UserViewModel
    {
        public long ID { get; set; }

        [Required, MinLength(6), DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        public string Username { get; set; }
        public bool IsAdmin { get; set; }
        public string SSHPublicKey { get; set; }
        public bool Active { get; set; }
        public long RoleID { get; set; }
        public DateTime LastUsed { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public UserInformation UserInformation { get; set; }
    }
}
