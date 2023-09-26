using EnvironmentServer.Util;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories;

public class Shopware6VersionRepository
{
	private Database DB;

	public Shopware6VersionRepository(Database db)
	{
		DB = db;
	}

    public async Task<IEnumerable<string>> GetVersionsAsync()
    {
        //---- not working due to bugged httpClient ----
        //HttpClient client = new HttpClient();
        //HttpResponseMessage response = await client.GetAsync("https://releases.shopware.com/changelog/index.json");
        //response.EnsureSuccessStatusCode();
        //string responseBody = await response.Content.ReadAsStringAsync();

        var versions = await Bash.CommandQueryAsync("curl https://releases.shopware.com/changelog/index.json", "/", true);
        
        return JsonConvert.DeserializeObject<IEnumerable<string>>(versions.ToString());
    }
}