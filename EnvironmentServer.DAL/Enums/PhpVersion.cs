﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Enums
{
    public enum PhpVersion
    {
        Php56,
        Php74,
        Php80,
        Php81
    }

    public static class PhpVersionExtensions
    {
        public static string AsString(this PhpVersion v) => v switch {
            PhpVersion.Php56 => "php5.6-fpm",
            PhpVersion.Php74 => "php7.4-fpm",
            PhpVersion.Php80 => "php8.0-fpm",
            PhpVersion.Php81 => "php8.1-fpm",
            _ => throw new InvalidOperationException("Unkown Php Version: " + v)
        };
    }
}