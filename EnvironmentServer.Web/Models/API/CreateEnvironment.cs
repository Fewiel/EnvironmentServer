namespace EnvironmentServer.Web.Models.API;

public class CreateEnvironment
{
    public long AccountID { get; set; }
    public string AccountMail { get; set; }
    public string ExtensionName { get; set; }
    public string Base64Extension { get; set; }
    public string ShopwareVersion { get; set; }
}