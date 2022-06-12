using Org.BouncyCastle.Asn1.Cms;

namespace EnvironmentServer.DAL.Models;

public class RoleLimit
{
    public long RoleID { get; set; }
    public long LimitID { get; set; }
    public int Value { get; set; }
}