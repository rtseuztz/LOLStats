using sqltest;
using System.Data;
namespace dotnet_react_typescript;
public class Summoner
{
    public string Name { get; private set; }
    public string Puuid { get; private set; }
    public string Id { get; private set; }
    public string AccountId { get; private set; }
    public string ProfileIconId { get; private set; }
    public string RevisionDate { get; private set; }
    public string SummonerLevel { get; private set; }
    private static string apikey = Environment.GetEnvironmentVariable("RIOT_API_KEY");
    public Summoner(string name, string puuid, string id, string accountId, string profileIconId, string revisionDate, string summonerLevel)
    {
        Name = name;
        Puuid = puuid;
        Id = id;
        AccountId = accountId;
        ProfileIconId = profileIconId;
        RevisionDate = revisionDate;
        SummonerLevel = summonerLevel;
    }
    public static Summoner? GetSummoner(string name)
    {
        using (var webClient = new System.Net.WebClient())
        {
            try
            {
                string url = $"https://na1.api.riotgames.com/lol/summoner/v4/summoners/by-name/{name}?api_key={Summoner.apikey}";
                var json = webClient.DownloadString(url);
                Summoner summoner = Newtonsoft.Json.JsonConvert.DeserializeObject<Summoner>(json);
                return summoner;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

    }
    public async Task<float> winRate()
    {
        try
        {
            DataTable dt = await sqltest.SQL.executeQuery(@"
            DECLARE @gameCount AS int = (
                SELECT COUNT(*) FROM ChampionPlayedIn WHERE puuid = @puuid
            )
            DECLARE @wins AS INT = (
                SELECT COUNT(*) FROM ChampionPlayedIn WHERE puuid = @puuid AND win = 1
            )
            DECLARE @winrate AS FLOAT = CAST(@wins as float)/CAST(@gameCount as float)
            SELECT @winrate", sqltest.SQL.getParams(new dynamic[] { "puuid", Puuid }));
            if (dt.Rows.Count > 0)
            {
                return float.Parse(dt.Rows[0][0].ToString());
            }
            else
            {
                return 0;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 0;
        }
    }
    public async void updateUploadSummoner()
    {
        try
        {

            DateTime twoDaysAgo = DateTime.Now.AddDays(-2);
            await sqltest.SQL.executeQuery(@"
            IF NOT EXISTS (
                SELECT 1 FROM Summoners WHERE puuid = @puuid
            )
            BEGIN
                INSERT INTO Summoners (id, name, puuid, accountId, profileIconId, revisionDate, summonerLevel, processed, lmod) VALUES (@id, @name, @puuid, @accountId, @profileIconId, @revisionDate, @summonerLevel, 1, @lmod)
            END
            ELSE
            BEGIN
                UPDATE Summoners SET name = @name, accountId = @accountId, profileIconId = @profileIconId, revisionDate = @revisionDate, summonerLevel = @summonerLevel, lmod = getDate(), processed = 1 WHERE puuid = @puuid
            END",
            sqltest.SQL.getParams(new dynamic[] { "id", Id, "name", Name, "puuid", Puuid.Trim(), "accountId", AccountId, "profileIconId", ProfileIconId, "revisionDate", RevisionDate, "summonerLevel", SummonerLevel, "lmod", twoDaysAgo }));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    private async void getRankAPI() {
        
    }
}
