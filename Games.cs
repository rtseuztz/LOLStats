using Newtonsoft.Json;
using sqltest;
namespace dotnet_react_typescript;

using System.Data;
public class Games
{
    public long gameCreation { get; set; }
    public long gameDuration { get; set; }
    public long gameEndTimestamp { get; set; }
    public long GameID { get; set; }
    public string gameMode { get; set; }
    public string gameName { get; set; }
    public long gameStartTimestamp { get; set; }
    public string gameType { get; set; }
    public string gameVersion { get; set; }
    public List<Participant> participants { get; set; }
    public long mapId { get; set; }

    //constructor with all the properties
    public Games(long gameCreation, long gameDuration, long gameEndTimestamp, long GameID, string gameMode, string gameName, long gameStartTimestamp, string gameType, string gameVersion, List<Participant> participants, long mapId)
    {
        this.gameCreation = gameCreation;
        this.gameDuration = gameDuration;
        this.gameEndTimestamp = gameEndTimestamp;
        this.GameID = GameID;
        this.gameMode = gameMode;
        this.gameName = gameName;
        this.gameStartTimestamp = gameStartTimestamp;
        this.gameType = gameType;
        this.gameVersion = gameVersion;
        this.participants = participants;
        this.mapId = mapId;
    }

    private static string apikey = Environment.GetEnvironmentVariable("RIOT_API_KEY");
    public async void uploadGame()
    {
        try
        {
            await SQL.executeQuery(@"
                INSERT INTO Games
                (gameCreation, gameDuration, gameEndTimestamp, GameID, gameMode, gameName, gameStartTimestamp, gameType, gameVersion, mapId) 
                VALUES
                (@gameCreation, @gameDuration, @gameEndTimestamp, @GameID, @gameMode, @gameName, @gameStartTimestamp, @gameType, @gameVersion, @mapId)",
                SQL.getParams(new dynamic[] { "gameCreation", gameCreation, "gameDuration", gameDuration, "gameEndTimestamp", gameEndTimestamp, "GameID", GameID, "gameMode", gameMode, "gameName", gameName, "gameStartTimestamp", gameStartTimestamp, "gameType", gameType, "gameVersion", gameVersion, "mapId", mapId }));

            foreach (Participant participant in participants)
            {
                await participant.checkUploadSummoner();
                await participant.uploadParticipant(GameID);
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
  
    public static async Task<List<Games>> getGames(string puuid)
    {
        List<Games> games = new List<Games>();
        string url = "https://americas.api.riotgames.com/lol/match/v5/matches/by-puuid/" + puuid + "/ids?start=0&count=20&api_key=" + apikey;
        List<string> GameIDs = new List<string>();
        using (var webClient = new System.Net.WebClient())
        {
            string json = webClient.DownloadString(url);
            GameIDs = JsonConvert.DeserializeObject<List<string>>(json);
        }
        for (int i = 0; i < GameIDs.Count; i++)
        {
            //remove everything before and including the _ in the game id
            GameIDs[i] = GameIDs[i].Substring(GameIDs[i].IndexOf("_") + 1);
        }
        try
        {
            List<string> GameIDsInDB = new List<string>();
            DataTable gidDt = await SQL.executeQuery(@"
                SELECT GameID FROM Games WHERE GameID IN (" + string.Join(",", GameIDs) + ")");
            foreach (DataRow row in gidDt.Rows)
            {
                GameIDsInDB.Add(row[0].ToString());
            }
            List<string> gamesMissing = GameIDs.Where(x => !GameIDsInDB.Contains(x))
                         .ToList();
            List<string> gamesInDB = GameIDs.Where(x => GameIDsInDB.Contains(x))
                         .ToList();
            games.AddRange(await getGamesFromAPI(gamesMissing));
            games.AddRange(await getGamesFromDB(gamesInDB));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return games;
    }
    private async static Task<List<Games>> getGamesFromDB(List<string> GameIDs)
    {
        List<Games> games = new List<Games>();
        try
        {
            if (GameIDs.Count < 1)
            {
                return games;
            }
            DataTable dt = await SQL.executeQuery(@"
                SELECT * FROM Games WHERE GameID IN (" + string.Join(",", GameIDs) + ") ORDER BY gameStartTimestamp DESC");
            foreach (DataRow row in dt.Rows)
            {
                List<Participant> participants = await Participant.getParticipants(long.Parse(row["GameID"].ToString()));
                games.Add(new Games(
                    long.Parse(row["gameCreation"].ToString()),
                    long.Parse(row["gameDuration"].ToString()),
                    long.Parse(row["gameEndTimestamp"].ToString()),
                    long.Parse(row["GameID"].ToString()),
                    row["gameMode"].ToString(),
                    row["gameName"].ToString(),
                    long.Parse(row["gameStartTimestamp"].ToString()),
                    row["gameType"].ToString(),
                    row["gameVersion"].ToString(),
                    participants,
                    long.Parse(row["mapId"].ToString())
                ));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return games;
    }
    private static async Task<List<Games>> getGamesFromAPI(List<String> GameIDs)
    {
        List<Games> games = new List<Games>();
        using (var webClient = new System.Net.WebClient())
        {
            foreach (string GameID in GameIDs)
            {
                string url2 = "https://americas.api.riotgames.com/lol/match/v5/matches/NA1_" + GameID + "?api_key=" + apikey;
                string json2 = webClient.DownloadString(url2);
                gameJSON gamejson = JsonConvert.DeserializeObject<gameJSON>(json2);
                Games game = new Games(gamejson.info.gameCreation, gamejson.info.gameDuration, gamejson.info.gameEndTimestamp, gamejson.info.GameID, gamejson.info.gameMode, gamejson.info.gameName, gamejson.info.gameStartTimestamp, gamejson.info.gameType, gamejson.info.gameVersion, gamejson.info.participants, gamejson.info.mapId);
                games.Add(game);
                game.uploadGame();
            }
            return games;
        }
    }
}
public class gameJSON
{
    public Games info { get; set; }
}
public class Participant
{
    public int assists { get; set; }
    public int baronKills { get; set; }
    public int bountyLevel { get; set; }
    public int champExperience { get; set; }
    public int champLevel { get; set; }
    public int championId { get; set; }
    public string championName { get; set; }
    public string championTransform { get; set; }
    public int consumablesPurchased { get; set; }
    public int damageDealtToBuildings { get; set; }
    public int damageDealtToObjectives { get; set; }
    public int damageDealtToTurrets { get; set; }
    public int damageSelfMitigated { get; set; }
    public int deaths { get; set; }
    public int detectorWardsPlaced { get; set; }
    public int doubleKills { get; set; }
    public int dragonKills { get; set; }
    public Boolean firstBloodAssist { get; set; }
    public Boolean firstBloodKill { get; set; }
    public Boolean firstTowerAssist { get; set; }
    public Boolean firstTowerKill { get; set; }
    public Boolean gameEndedInEarlySurrender { get; set; }
    public Boolean gameEndedInSurrender { get; set; }
    public int goldEarned { get; set; }
    public int goldSpent { get; set; }
    public string individualPosition { get; set; }
    public int inhibitorKills { get; set; }

    private string _puuid { get; set; }
    public string puuid {
        get
        {
            return _puuid.Trim();
        }
        set
        {
            _puuid = value.Trim();
        }
    }
    public Boolean win { get; set; }
    public long gid { get; set; }

    //constructor with all parameters
    public Participant(int assists, int baronKills, int bountyLevel, int champExperience, int champLevel, int championId, string championName, string championTransform, int consumablesPurchased, int damageDealtToBuildings, int damageDealtToObjectives, int damageDealtToTurrets, int damageSelfMitigated, int deaths, int detectorWardsPlaced, int doubleKills, int dragonKills, Boolean firstBloodAssist, Boolean firstBloodKill, Boolean firstTowerAssist, Boolean firstTowerKill, Boolean gameEndedInEarlySurrender, Boolean gameEndedInSurrender, int goldEarned, int goldSpent, string individualPosition, int inhibitorKills, string puuid, Boolean win, long gid)
    {
        this.assists = assists;
        this.baronKills = baronKills;
        this.bountyLevel = bountyLevel;
        this.champExperience = champExperience;
        this.champLevel = champLevel;
        this.championId = championId;
        this.championName = championName;
        this.championTransform = championTransform;
        this.consumablesPurchased = consumablesPurchased;
        this.damageDealtToBuildings = damageDealtToBuildings;
        this.damageDealtToObjectives = damageDealtToObjectives;
        this.damageDealtToTurrets = damageDealtToTurrets;
        this.damageSelfMitigated = damageSelfMitigated;
        this.deaths = deaths;
        this.detectorWardsPlaced = detectorWardsPlaced;
        this.doubleKills = doubleKills;
        this.dragonKills = dragonKills;
        this.firstBloodAssist = firstBloodAssist;
        this.firstBloodKill = firstBloodKill;
        this.firstTowerAssist = firstTowerAssist;
        this.firstTowerKill = firstTowerKill;
        this.gameEndedInEarlySurrender = gameEndedInEarlySurrender;
        this.gameEndedInSurrender = gameEndedInSurrender;
        this.goldEarned = goldEarned;
        this.goldSpent = goldSpent;
        this.individualPosition = individualPosition;
        this.inhibitorKills = inhibitorKills;
        this.puuid = puuid;
        this.win = win;
        this.gid = gid;
    }
    public async Task<Boolean> checkUploadSummoner()
    {
        try
        {
            await SQL.executeQuery(@"
            IF NOT EXISTS (
                SELECT 1
                FROM summoners
                WHERE puuid = @puuid
            )
            BEGIN
                INSERT INTO summoners (puuid)
                VALUES (@puuid)
            END",
            SQL.getParams(new dynamic[] {
                "puuid", puuid,
            }));
            return true;

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
    public async Task<Boolean> uploadParticipant(long GameID)
    {
        try
        {
            var x = await SQL.executeQuery(@"
            INSERT INTO ChampionPlayedIn
            (gid, cname, win, puuid, assists, baronKills, bountyLevel, champExperience, champLevel, championId, championTransform, consumablesPurchased, damageDealtToBuildings, damageDealtToObjectives, damageDealtToTurrets, damageSelfMitigated, deaths, detectorWardsPlaced, doubleKills, dragonKills, firstBloodAssist, firstBloodKill, firstTowerAssist, firstTowerKill, gameEndedInEarlySurrender, gameEndedInSurrender, goldEarned, goldSpent, individualPosition, inhibitorKills)
            VALUES
            (@GameID, @championName, @win, @puuid, @assists, @baronKills, @bountyLevel, @champExperience, @champLevel, @championId, @championTransform, @consumablesPurchased, @damageDealtToBuildings, @damageDealtToObjectives, @damageDealtToTurrets, @damageSelfMitigated, @deaths, @detectorWardsPlaced, @doubleKills, @dragonKills, @firstBloodAssist, @firstBloodKill, @firstTowerAssist, @firstTowerKill, @gameEndedInEarlySurrender, @gameEndedInSurrender, @goldEarned, @goldSpent, @individualPosition, @inhibitorKills)",
            SQL.getParams(new dynamic[] {
            "GameID", GameID ,
            "championName", championName,
            "win", win,
            "puuid", puuid.Trim(),
            "assists", assists,
            "baronKills", baronKills,
            "bountyLevel", bountyLevel,
            "champExperience", champExperience,
            "champLevel", champLevel,
            "championId", championId,
            "championTransform", championTransform,
            "consumablesPurchased", consumablesPurchased,
            "damageDealtToBuildings", damageDealtToBuildings,
            "damageDealtToObjectives", damageDealtToObjectives,
            "damageDealtToTurrets", damageDealtToTurrets,
            "damageSelfMitigated", damageSelfMitigated,
            "deaths", deaths,
            "detectorWardsPlaced", detectorWardsPlaced,
            "doubleKills", doubleKills,
            "dragonKills", dragonKills,
            "firstBloodAssist", firstBloodAssist,
            "firstBloodKill", firstBloodKill,
            "firstTowerAssist", firstTowerAssist,
            "firstTowerKill", firstTowerKill,
            "gameEndedInEarlySurrender", gameEndedInEarlySurrender,
            "gameEndedInSurrender", gameEndedInSurrender,
            "goldEarned", goldEarned,
            "goldSpent", goldSpent,
            "individualPosition", individualPosition,
            "inhibitorKills", inhibitorKills
           })
           );
            Console.WriteLine(x);
            return true;

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
    public static async Task<List<Participant>> getParticipants(long GameID)
    {
        List<Participant> participants = new List<Participant>();
        try
        {
            DataTable dt = await SQL.executeQuery(@"
            SELECT * FROM ChampionPlayedIn
            WHERE gid = @GameID",
            SQL.getParams(new dynamic[] { "GameID", GameID })
            );
            participants = Participant.listFromDT(dt);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return participants;
    }
    private static List<Participant> listFromDT(DataTable dt)
    {
        List<Participant> participants = new List<Participant>();
        foreach (DataRow row in dt.Rows)
        {
            participants.Add(objFromRow(row));
        }
        return participants;
    }
    private static Participant objFromRow(DataRow row)
    {
        Participant participant = new Participant(
                Convert.ToInt32(row["assists"]),
                Convert.ToInt32(row["baronKills"]),
                Convert.ToInt32(row["bountyLevel"]),
                Convert.ToInt32(row["champExperience"]),
                Convert.ToInt32(row["champLevel"]),
                Convert.ToInt32(row["championId"]),
                Convert.ToString(row["cname"]),
                Convert.ToString(row["championTransform"]),
                Convert.ToInt32(row["consumablesPurchased"]),
                Convert.ToInt32(row["damageDealtToBuildings"]),
                Convert.ToInt32(row["damageDealtToObjectives"]),
                Convert.ToInt32(row["damageDealtToTurrets"]),
                Convert.ToInt32(row["damageSelfMitigated"]),
                Convert.ToInt32(row["deaths"]),
                Convert.ToInt32(row["detectorWardsPlaced"]),
                Convert.ToInt32(row["doubleKills"]),
                Convert.ToInt32(row["dragonKills"]),
                Convert.ToBoolean(row["firstBloodAssist"]),
                Convert.ToBoolean(row["firstBloodKill"]),
                Convert.ToBoolean(row["firstTowerAssist"]),
                Convert.ToBoolean(row["firstTowerKill"]),
                Convert.ToBoolean(row["gameEndedInEarlySurrender"]),
                Convert.ToBoolean(row["gameEndedInSurrender"]),
                Convert.ToInt32(row["goldEarned"]),
                Convert.ToInt32(row["goldSpent"]),
                Convert.ToString(row["individualPosition"]),
                Convert.ToInt32(row["inhibitorKills"]),
                Convert.ToString(row["puuid"]).Trim(),
                Convert.ToBoolean(row["win"]),
                Convert.ToInt64(row["gid"])
                );
        return participant;
    }
}