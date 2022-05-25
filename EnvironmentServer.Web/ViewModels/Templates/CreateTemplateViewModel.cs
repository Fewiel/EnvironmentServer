using System.ComponentModel.DataAnnotations;

namespace EnvironmentServer.Web.ViewModels.Templates
{
    public class CreateTemplateViewModel
    {
        [MinLength(4), MaxLength(24), DataType(DataType.Text)]
        public string Name { get; set; }

        [MinLength(4), DataType(DataType.Text)]
        public string Descirption { get; set; }
        public long EnvironmentID { get; set; }
    }
}