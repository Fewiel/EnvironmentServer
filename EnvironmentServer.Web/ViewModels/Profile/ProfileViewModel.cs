﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.ViewModels.Profile
{
    public class ProfileViewModel
    {
        [MinLength(4), DataType(DataType.Password)]
        public string Password { get; set; }

        [MinLength(6), DataType(DataType.Password)]
        [RegularExpression(@"^[^'"" ´`]*$", ErrorMessage = "Password may not contain: ^'\"´`[SPACE]")]
        public string PasswordNew { get; set; }

        [MinLength(6), DataType(DataType.Password)]
        [RegularExpression(@"^[^'"" ´`]*$", ErrorMessage = "Password may not contain: ^'\"´`[SPACE]")]
        public string PasswordNewRetype { get; set; }
    }
}
