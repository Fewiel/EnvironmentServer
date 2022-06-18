using System;

namespace EnvironmentServer.Web.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class PermissionAttribute : Attribute
{
    public string PermissionName { get; }

    public PermissionAttribute(string permissionName)
    {
        PermissionName = permissionName;
    }
}