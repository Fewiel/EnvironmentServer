﻿using EnvironmentServer.DAL.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.ViewModels.Login
{
    public class RegistrationViewModel
    {
        [MinLength(4), MaxLength(14), DataType(DataType.Text)]
        [RegularExpression(@"^[a-z_]([a-z0-9_-]{0,31}|[a-z0-9_-]{0,30}\$)$", ErrorMessage = "Username not allowed")]
        public string Username { get; set; }

        [MinLength(4), DataType(DataType.Password)]
        [RegularExpression(@"^[^'"" $´`]*$", ErrorMessage = "Password may not contain: ^'\"$´`[SPACE]")]
        public string Password { get; set; }

        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}")]
        public DateTime? ExpirationDate { get; set; }
        
        public long RoleID { get; set; }

        public List<SelectListItem> Roles { get; set; }        
    }
}
