using EnvironmentServer.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http;
using Newtonsoft.Json;
using EnvironmentServer.DAL.Models;

namespace EnvironmentServer.Daemon.ScheduleActions
{
    internal class TagsRefresh : ScheduledActionBase
    {
        public TagsRefresh(ServiceProvider sp) : base(sp) { }

        public override string ActionIdentifier => "tags_refresh";

        public override async Task ExecuteAsync(Database db)
        {
            await GatherTagsAsync("https://api.github.com/repos/shopware/shopware/git/refs/tags", db);
            await GatherTagsAsync("https://api.github.com/repos/shopware/platform/git/refs/tags", db);
        }

        private async Task GatherTagsAsync(string url, Database db)
        {
            var client = SP.GetService<HttpClient>();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<TagReference>>(json);

            foreach (var r in result)
            {
                db.TagCache.CreateIfNotExist(new Tag
                {
                    Name = r.Ref.Replace("refs/tags/", "").TrimStart('v'),
                    Hash = r.Object.sha
                });
            }
        }
    }

    internal class TagReference
    {
        public string Ref { get; set; }
        public string node_id { get; set; }
        public string url { get; set; }
        public Payload Object { get; set; }
    }

    internal class Payload
    {
        public string sha { get; set; }
        public string type { get; set; }
        public string url { get; set; }
    }

}
