using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
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
        HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync("https://releases.shopware.com/changelog/index.json");
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<IEnumerable<string>>(responseBody);
    }
}