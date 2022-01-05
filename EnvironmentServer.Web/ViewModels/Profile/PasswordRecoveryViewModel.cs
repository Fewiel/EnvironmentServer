using System.ComponentModel.DataAnnotations;

namespace EnvironmentServer.Web.ViewModels.Profile;

public class PasswordRecoveryViewModel
{
    [DataType(DataType.EmailAddress)]
    public string Mail { get; set; }

    public string Token { get; set; }

    [MinLength(6), DataType(DataType.Password)]
    [RegularExpression(@"^[^'"" ´`]*$", ErrorMessage = "Password may not contain: ^'\"´`[SPACE]")]
    public string PasswordNew { get; set; }

    [MinLength(6), DataType(DataType.Password)]
    [RegularExpression(@"^[^'"" ´`]*$", ErrorMessage = "Password may not contain: ^'\"´`[SPACE]")]
    public string PasswordNewRetype { get; set; }
}
