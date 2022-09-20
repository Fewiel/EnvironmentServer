using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Models;
using EnvironmentServer.Web.ViewModels.Docker;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers
{
    public class DockerController : ControllerBase
    {
        public DockerController(Database db) : base(db)
        {
        }

        public async Task<IActionResult> IndexAsync()
        {
            List<DockerContainer> containers = (List<DockerContainer>)await DB.DockerContainer.GetAllForUserAsync(GetSessionUser().ID);
            List<DockerContainerData> dData = new();

            foreach (var c in containers)
            {
                dData.Add(new() { Container = c, ContainerPorts = DB.DockerPort.GetForContainer(c.ID) });
            }

            DockerIndexViewModel dview = new() { Containers = dData };

            return View(dview);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAsync()
        {
            CreateContainerViewModel ccvm = new()
            {
                Container = new(),
                ComposeFiles = await DB.DockerComposeFile.GetAllAsync()
            };

            return View(ccvm);
        }

        public async Task<IActionResult> CreateAsync([FromForm] CreateContainerViewModel ccvm)
        {
            var container = new DockerContainer { Name = ccvm.Container.Name, UserID = GetSessionUser().ID, DockerComposeFileID = ccvm.Container.DockerComposeFileID };

            var id = await DB.DockerContainer.InsertAsync(container);
            DB.CmdAction.CreateTask(new CmdAction { Action = "docker.create", Id_Variable = id });

            return RedirectToAction("Index");
        }


        [HttpGet]
        public IActionResult CreateComposerFile()
        {
            return View(new DockerComposeFile());
        }

        public async Task<IActionResult> CreateComposerFile([FromForm] DockerComposeFile cf)
        {
            var usr = GetSessionUser();
            cf.UserID = usr.ID;

            if (string.IsNullOrEmpty(cf.Name) || string.IsNullOrEmpty(cf.Description) || string.IsNullOrEmpty(cf.FileContent))
            {
                AddError("Please fill out all fields");
                return View(cf);
            }

            await DB.DockerComposeFile.InsertAsync(cf);
            AddInfo("Composer File added");
            return View(new DockerComposeFile());
        }

        public async Task<IActionResult> StartAsync(long id)
        {
            DockerContainer container = await DB.DockerContainer.GetByIDAsync(id);

            if (container == null)
                return RedirectToAction("Index");

            if (!container.Active)
            {
                DB.CmdAction.CreateTask(new CmdAction() { Action = "docker.start", Id_Variable = id });
            }
            else
            {
                DB.CmdAction.CreateTask(new CmdAction() { Action = "docker.stop", Id_Variable = id });
            }

            return RedirectToAction("Index");
        }
    }
}