using EnvironmentServer.DAL.Models;
using System.Collections.Generic;

namespace EnvironmentServer.Web.ViewModels.Templates;

public class IndexTemplateViewModel
{
    public IEnumerable<Template> Templates { get; set; }
}