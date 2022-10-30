using System.Net.Http.Headers;

namespace Util;
public class Util
{
    public static string getLatestVersion()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri("https://ddragon.leagueoflegends.com");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        const string defaultVersion = "12.1.1";
        HttpResponseMessage response = client.GetAsync("/api/versions.json").Result;
        if (response.IsSuccessStatusCode)
        {
            var versions = response.Content.ReadFromJsonAsync<List<string>>().Result;
            return versions?[0] ?? defaultVersion;
        }
        return defaultVersion;
    }
}