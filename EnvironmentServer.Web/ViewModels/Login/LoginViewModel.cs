﻿using EnvironmentServer.DAL.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.ViewModels.Login
{
    public class LoginViewModel
    {
        [Required, MinLength(4), MaxLength(32), DataType(DataType.Text)]
        public string Username { get; set; }

        [Required, MinLength(4), DataType(DataType.Password)]
        public string Password { get; set; }

        public IEnumerable<News> LatestNews { get; set; }
    }
}
