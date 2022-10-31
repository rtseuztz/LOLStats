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
    public int inhibitorTakedowns { get; set; }
    public int inhibitorsLost { get; set; }
    public int item0 { get; set; }
    public int item1 { get; set; }
    public int item2 { get; set; }
    public int item3 { get; set; }
    public int item4 { get; set; }
    public int item5 { get; set; }
    public int item6 { get; set; }
    public int itemsPurchased { get; set; }
    public int killingSprees { get; set; }
    public int kills { get; set; }
    public string lane { get; set; }
    public int largestCriticalStrike { get; set; }
    public int largestKillingSpree { get; set; }
    public int largestMultiKill { get; set; }
    public int longestTimeSpentLiving { get; set; }
    public int magicDamageDealt { get; set; }
    public int magicDamageDealtToChampions { get; set; }
    public int magicDamageTaken { get; set; }
    public int neutralMinionsKilled { get; set; }
    public int nexusKills { get; set; }
    public int nexusTakedowns { get; set; }
    public int nexusLost { get; set; }
    public int objectivesStolen { get; set; }
    public int objectivesStolenAssists { get; set; }
    public int participantId { get; set; }
    public int pentaKills { get; set; }
    // public int perk0 { get; set; }
    // public int perk0Var1 { get; set; }
    // public int perk0Var2 { get; set; }
    // public int perk0Var3 { get; set; }
    // public int perk1 { get; set; }
    // public int perk1Var1 { get; set; }
    // public int perk1Var2 { get; set; }
    // public int perk1Var3 { get; set; }
    // public int perk2 { get; set; }
    // public int perk2Var1 { get; set; }
    // public int perk2Var2 { get; set; }
    // public int perk2Var3 { get; set; }
    // public int perk3 { get; set; }
    // public int perk3Var1 { get; set; }
    // public int perk3Var2 { get; set; }
    // public int perk3Var3 { get; set; }
    // public int perk4 { get; set; }
    // public int perk4Var1 { get; set; }
    // public int perk4Var2 { get; set; }
    // public int perk4Var3 { get; set; }
    // public int perk5 { get; set; }
    // public int perk5Var1 { get; set; }
    // public int perk5Var2 { get; set; }
    // public int perk5Var3 { get; set; }
    // public int perkPrimaryStyle { get; set; }
    // public int perkSubStyle { get; set; }
    public int physicalDamageDealt { get; set; }
    public int physicalDamageDealtToChampions { get; set; }
    public int physicalDamageTaken { get; set; }
    public int profileIcon { get; set; }
    //public string puuid { get; set; }
    public int quadraKills { get; set; }
    public string riotIdName { get; set; }
    public string riotIdTagline { get; set; }
    public string role { get; set; }
    public int sightWardsBoughtInGame { get; set; }
    public int spell1Casts { get; set; }
    public int spell2Casts { get; set; }
    public int spell3Casts { get; set; }
    public int spell4Casts { get; set; }
    public int summoner1Casts { get; set; }
    public int summoner1Id { get; set; }
    public int summoner2Casts { get; set; }
    public int summoner2Id { get; set; }
    public string summonerId { get; set; }
    public string summonerName { get; set; }
    public bool teamEarlySurrendered { get; set; }
    public int teamId { get; set; }
    public string teamPosition { get; set; }
    public int timeCCingOthers { get; set; }
    public int timePlayed { get; set; }
    public int totalDamageDealt { get; set; }
    public int totalDamageDealtToChampions { get; set; }
    public int totalDamageShieldedOnTeammates { get; set; }
    public int totalDamageTaken { get; set; }
    public int totalHeal { get; set; }
    public int totalHealsOnTeammates { get; set; }
    public int totalMinionsKilled { get; set; }
    public int totalTimeCCDealt { get; set; }
    public int totalTimeSpentDead { get; set; }
    public int totalUnitsHealed { get; set; }
    public int tripleKills { get; set; }
    public int trueDamageDealt { get; set; }
    public int trueDamageDealtToChampions { get; set; }
    public int trueDamageTaken { get; set; }
    public int turretKills { get; set; }
    public int turretTakedowns { get; set; }
    public int turretsLost { get; set; }
    public int unrealKills { get; set; }
    public int visionScore { get; set; }
    public int visionWardsBoughtInGame { get; set; }
    public int wardsKilled { get; set; }
    public int wardsPlaced { get; set; }
    private string _puuid { get; set; }
    private int summonerLevel { get; set; }
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

    // //constructor for the class with all of the parameters
    // public Participant(int assists,
    //                     int baronKills,
    //     int bountyLevel,
    //     int champExperience,
    //     int champLevel,
    //     int championId,
    //     int championTransform,
    //     int consumablesPurchased,
    //     int damageDealtToBuildings,
    //     int damageDealtToObjectives,
    //     int damageDealtToTurrets,
    //     int damageSelfMitigated,
    //     int deaths,
    //     int detectorWardsPlaced,
    //     int doubleKills,
    //     int dragonKills,
    //     int firstBloodAssist,
    //     int firstBloodKill,
    //     int firstTowerAssist,
    //     int firstTowerKill,
    //     int gameEndedInEarlySurrender,
    //     int gameEndedInSurrender,
    //     int goldEarned,
    //     int goldSpent,
    //     int individualPosition,
    //     int inhibitorKills,
    //     int inhibitorTakedowns,
    //     int inhibitorsLost,
    //     int item0,
    //     int item1,
    //     int item2,
    //     int item3,
    //     int item4,
    //     int item5,
    //     int item6,
    //     int itemsPurchased,
    //     int killingSprees,
    //     int kills,
    //     int lane,
    //     int largestCriticalStrike,
    //     int largestKillingSpree,
    //     int largestMultiKill,
    //     int longestTimeSpentLiving,
    //     int magicDamageDealt,
    //     int magicDamageDealtToChampions,
    //     int magicDamageTaken,
    //     int neutralMinionsKilled,
    //     int nexusKills,
    //     int nexusTakedowns,
    //     int nexusLost,
    //     int objectivesStolen,
    //     int objectivesStolenAssists,
    //     int participantId,
    //     int pentaKills,
    //     // int perk0,
    //     // int perk0Var1,
    //     // int perk0Var2,
    //     // int perk0Var3,
    //     // int perk1,
    //     // int perk1Var1,
    //     // int perk1Var2,
    //     // int perk1Var3,
    //     // int perk2,
    //     // int perk2Var1,
    //     // int perk2Var2,
    //     // int perk2Var3,
    //     // int perk3,
    //     // int perk3Var1,
    //     // int perk3Var2,
    //     // int perk3Var3,
    //     // int perk4,
    //     // int perk4Var1,
    //     // int perk4Var2,
    //     // int perk4Var3,
    //     // int perk5,
    //     // int perk5Var1,
    //     // int perk5Var2,
    //     // int perk5Var3,
    //     // int perkPrimaryStyle,
    //     // int perkSubStyle,
    //     int physicalDamageDealt,
    //     int physicalDamageDealtToChampions,
    //     int physicalDamageTaken,
    //     int profileIcon,
    //     // string puuid,
    //     int quadraKills,
    //     int riotIdName,
    //     int riotIdTagline,
    //     int role,
    //     int sightWardsBoughtInGame,
    //     int spell1Casts,
    //     int spell2Casts,
    //     int spell3Casts,
    //     int spell4Casts,
    //     int summoner1Casts,
    //     int summoner1Id,
    //     int summoner2Casts,
    //     int summoner2Id,
    //     string summonerId,
    //     string summonerName,
    //     int teamEarlySurrendered,
    //     int teamId,
    //     int teamPosition,
    //     int timeCCingOthers,
    //     int timePlayed,
    //     int totalDamageDealt,
    //     int totalDamageDealtToChampions,
    //     int totalDamageShieldedOnTeammates,
    //     int totalDamageTaken,
    //     int totalHeal,
    //     int totalHealsOnTeammates,
    //     int totalMinionsKilled,
    //     int totalTimeCCDealt,
    //     int totalTimeSpentDead,
    //     int totalUnitsHealed,
    //     int tripleKills,
    //     int trueDamageDealt,
    //     int trueDamageDealtToChampions,
    //     int trueDamageTaken,
    //     int turretKills,
    //     int turretTakedowns,
    //     int turretsLost,
    //     int unrealKills,
    //     int visionScore,
    //     int visionWardsBoughtInGame,
    //     int wardsKilled,
    //     int wardsPlaced,
    //     string puuid,
    //     int summonerLevel,
    //     Boolean win,
    //     long gid)

    // {
    //     this.assists = assists;
    //     this.baronKills = baronKills;
    //     this.bountyLevel = bountyLevel;
    //     this.champExperience = champExperience;
    //     this.champLevel = champLevel;
    //     this.championId = championId;
    //     this.championTransform = championTransform;
    //     this.consumablesPurchased = consumablesPurchased;
    //     this.damageDealtToBuildings = damageDealtToBuildings;
    //     this.damageDealtToObjectives = damageDealtToObjectives;
    //     this.damageDealtToTurrets = damageDealtToTurrets;
    //     this.damageSelfMitigated = damageSelfMitigated;
    //     this.deaths = deaths;
    //     this.detectorWardsPlaced = detectorWardsPlaced;
    //     this.doubleKills = doubleKills;
    //     this.dragonKills = dragonKills;
    //     this.firstBloodAssist = firstBloodAssist;
    //     this.firstBloodKill = firstBloodKill;
    //     this.firstTowerAssist = firstTowerAssist;
    //     this.firstTowerKill = firstTowerKill;
    //     this.gameEndedInEarlySurrender = gameEndedInEarlySurrender;
    //     this.gameEndedInSurrender = gameEndedInSurrender;
    //     this.goldEarned = goldEarned;
    //     this.goldSpent = goldSpent;
    //     this.individualPosition = individualPosition;
    //     this.inhibitorKills = inhibitorKills;
    //     this.inhibitorTakedowns = inhibitorTakedowns;
    //     this.inhibitorsLost = inhibitorsLost;
    //     this.item0 = item0;
    //     this.item1 = item1;
    //     this.item2 = item2;
    //     this.item3 = item3;
    //     this.item4 = item4;
    //     this.item5 = item5;
    //     this.item6 = item6;
    //     this.itemsPurchased = itemsPurchased;
    //     this.killingSprees = killingSprees;
    //     this.kills = kills;
    //     this.lane = lane;
    //     this.largestCriticalStrike = largestCriticalStrike;
    //     this.largestKillingSpree = largestKillingSpree;
    //     this.largestMultiKill = largestMultiKill;
    //     this.longestTimeSpentLiving = longestTimeSpentLiving;
    //     this.magicDamageDealt = magicDamageDealt;
    //     this.magicDamageDealtToChampions = magicDamageDealtToChampions;
    //     this.magicDamageTaken = magicDamageTaken;
    //     this.neutralMinionsKilled = neutralMinionsKilled;
    // }

    //constructor with all parameters
    public Participant(int assists,
                        int baronKills,
        int bountyLevel,
        int champExperience,
        int champLevel,
        int championId,
        string championName,
        string championTransform,
        int consumablesPurchased,
        int damageDealtToBuildings,
        int damageDealtToObjectives,
        int damageDealtToTurrets,
        int damageSelfMitigated,
        int deaths,
        int detectorWardsPlaced,
        int doubleKills,
        int dragonKills,
        bool firstBloodAssist,
        bool firstBloodKill,
        bool firstTowerAssist,
        bool firstTowerKill,
        bool gameEndedInEarlySurrender,
        bool gameEndedInSurrender,
        int goldEarned,
        int goldSpent,
        string individualPosition,
        int inhibitorKills,
        int inhibitorTakedowns,
        int inhibitorsLost,
        int item0,
        int item1,
        int item2,
        int item3,
        int item4,
        int item5,
        int item6,
        int itemsPurchased,
        int killingSprees,
        int kills,
        string lane,
        int largestCriticalStrike,
        int largestKillingSpree,
        int largestMultiKill,
        int longestTimeSpentLiving,
        int magicDamageDealt,
        int magicDamageDealtToChampions,
        int magicDamageTaken,
        int neutralMinionsKilled,
        int nexusKills,
        int nexusTakedowns,
        int nexusLost,
        int objectivesStolen,
        int objectivesStolenAssists,
        int participantId,
        int pentaKills,
        // int perk0,
        // int perk0Var1,
        // int perk0Var2,
        // int perk0Var3,
        // int perk1,
        // int perk1Var1,
        // int perk1Var2,
        // int perk1Var3,
        // int perk2,
        // int perk2Var1,
        // int perk2Var2,
        // int perk2Var3,
        // int perk3,
        // int perk3Var1,
        // int perk3Var2,
        // int perk3Var3,
        // int perk4,
        // int perk4Var1,
        // int perk4Var2,
        // int perk4Var3,
        // int perk5,
        // int perk5Var1,
        // int perk5Var2,
        // int perk5Var3,
        // int perkPrimaryStyle,
        // int perkSubStyle,
        int physicalDamageDealt,
        int physicalDamageDealtToChampions,
        int physicalDamageTaken,
        int profileIcon,
        int quadraKills,
        string riotIdName,
        string riotIdTagline,
        string role,
        int sightWardsBoughtInGame,
        int spell1Casts,
        int spell2Casts,
        int spell3Casts,
        int spell4Casts,
        int summoner1Casts,
        int summoner1Id,
        int summoner2Casts,
        int summoner2Id,
        string summonerId,
        int summonerLevel,
        string summonerName,
        bool teamEarlySurrendered,
        int teamId,
        string teamPosition,
        int timeCCingOthers,
        int timePlayed,
        int totalDamageDealt,
        int totalDamageDealtToChampions,
        int totalDamageShieldedOnTeammates,
        int totalDamageTaken,
        int totalHeal,
        int totalHealsOnTeammates,
        int totalMinionsKilled,
        int totalTimeCCDealt,
        int totalTimeSpentDead,
        int totalUnitsHealed,
        int tripleKills,
        int trueDamageDealtToChampions,
        int trueDamageDealt,
        int trueDamageTaken,
        int turretKills,
        int turretTakedowns,
        int turretsLost,
        int unrealKills,
        int visionScore,
        int visionWardsBoughtInGame,
        int wardsKilled,
        int wardsPlaced,
        string puuid,
        Boolean win,
        long gid)
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
        this.inhibitorTakedowns = inhibitorTakedowns;
        this.inhibitorsLost = inhibitorsLost;
        this.item0 = item0;
        this.item1 = item1;
        this.item2 = item2;
        this.item3 = item3;
        this.item4 = item4;
        this.item5 = item5;
        this.item6 = item6;
        this.itemsPurchased = itemsPurchased;
        this.killingSprees = killingSprees;
        this.kills = kills;
        this.lane = lane;
        this.largestCriticalStrike = largestCriticalStrike;
        this.largestKillingSpree = largestKillingSpree;
        this.largestMultiKill = largestMultiKill;
        this.longestTimeSpentLiving = longestTimeSpentLiving;
        this.magicDamageDealt = magicDamageDealt;
        this.magicDamageDealtToChampions = magicDamageDealtToChampions;
        this.magicDamageTaken = magicDamageTaken;
        this.neutralMinionsKilled = neutralMinionsKilled;
        this.nexusKills = nexusKills;
        this.nexusTakedowns = nexusTakedowns;
        this.nexusLost = nexusLost;
        this.objectivesStolen = objectivesStolen;
        this.objectivesStolenAssists = objectivesStolenAssists;
        this.participantId = participantId;
        this.pentaKills = pentaKills;
        this.physicalDamageDealt = physicalDamageDealt;
        this.physicalDamageDealtToChampions = physicalDamageDealtToChampions;
        this.physicalDamageTaken = physicalDamageTaken;
        this.profileIcon = profileIcon;
        this.quadraKills = quadraKills;
        this.riotIdName = riotIdName;
        this.riotIdTagline = riotIdTagline;
        this.role = role;
        this.sightWardsBoughtInGame = sightWardsBoughtInGame;
        this.spell1Casts = spell1Casts;
        this.spell2Casts = spell2Casts;
        this.spell3Casts = spell3Casts;
        this.spell4Casts = spell4Casts;
        this.summoner1Casts = summoner1Casts;
        this.summoner1Id = summoner1Id;
        this.summoner2Casts = summoner2Casts;
        this.summoner2Id = summoner2Id;
        this.summonerId = summonerId;
        this.summonerLevel = summonerLevel;
        this.summonerName = summonerName;
        this.teamEarlySurrendered = teamEarlySurrendered;
        this.teamId = teamId;
        this.teamPosition = teamPosition;
        this.timeCCingOthers = timeCCingOthers;
        this.timePlayed = timePlayed;
        this.totalDamageDealt = totalDamageDealt;
        this.totalDamageDealtToChampions = totalDamageDealtToChampions;
        this.totalDamageShieldedOnTeammates = totalDamageShieldedOnTeammates;
        this.totalDamageTaken = totalDamageTaken;
        this.totalHeal = totalHeal;
        this.totalHealsOnTeammates = totalHealsOnTeammates;
        this.totalMinionsKilled = totalMinionsKilled;
        this.totalTimeCCDealt = totalTimeCCDealt;
        this.totalTimeSpentDead = totalTimeSpentDead;
        this.totalUnitsHealed = totalUnitsHealed;
        this.tripleKills = tripleKills;
        this.trueDamageDealt = trueDamageDealt;
        this.trueDamageDealtToChampions = trueDamageDealtToChampions;
        this.trueDamageTaken = trueDamageTaken;
        this.turretKills = turretKills;
        this.turretTakedowns = turretTakedowns;
        this.turretsLost = turretsLost;
        this.unrealKills = unrealKills;
        this.visionScore = visionScore;
        this.visionWardsBoughtInGame = visionWardsBoughtInGame;
        this.wardsKilled = wardsKilled;
        this.wardsPlaced = wardsPlaced;
        this.win = win;
        this.gid = gid;
        this.puuid = puuid;
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
            (gid, cname, win, puuid, assists, baronKills, bountyLevel, champExperience, champLevel, championId, championTransform, consumablesPurchased, damageDealtToBuildings, damageDealtToObjectives, damageDealtToTurrets, damageSelfMitigated, deaths, detectorWardsPlaced, doubleKills, dragonKills, firstBloodAssist, firstBloodKill, firstTowerAssist, firstTowerKill, gameEndedInEarlySurrender, gameEndedInSurrender, goldEarned, goldSpent, individualPosition, inhibitorKills, inhibitorTakedowns, inhibitorsLost, item0, item1, item2, item3, item4, item5, item6, itemsPurchased, killingSprees, kills, lane, largestCriticalStrike, largestKillingSpree, largestMultiKill, longestTimeSpentLiving, magicDamageDealt, magicDamageDealtToChampions, magicDamageTaken, neutralMinionsKilled, nexusKills, nexusTakedowns, nexusLost, objectivesStolen, objectivesStolenAssists, participantId, pentaKills, physicalDamageDealt, physicalDamageDealtToChampions, physicalDamageTaken, profileIcon, quadraKills, riotIdName, riotIdTagline, role, sightWardsBoughtInGame, spell1Casts, spell2Casts, spell3Casts, spell4Casts, summoner1Casts, summoner1Id, summoner2Casts, summoner2Id, summonerId, summonerLevel, summonerName, teamEarlySurrendered, teamId, teamPosition, timeCCingOthers, timePlayed, totalDamageDealt, totalDamageDealtToChampions, totalDamageShieldedOnTeammates, totalDamageTaken, totalHeal, totalHealsOnTeammates, totalMinionsKilled, totalTimeCCDealt, totalTimeSpentDead, totalUnitsHealed, tripleKills, trueDamageDealt, trueDamageDealtToChampions, trueDamageTaken, turretKills, turretTakedowns, turretsLost, unrealKills, visionScore, visionWardsBoughtInGame, wardsKilled, wardsPlaced)
            VALUES
            (@GameID, @championName, @win, @puuid, @assists, @baronKills, @bountyLevel, @champExperience, @champLevel, @championId, @championTransform, @consumablesPurchased, @damageDealtToBuildings, @damageDealtToObjectives, @damageDealtToTurrets, @damageSelfMitigated, @deaths, @detectorWardsPlaced, @doubleKills, @dragonKills, @firstBloodAssist, @firstBloodKill, @firstTowerAssist, @firstTowerKill, @gameEndedInEarlySurrender, @gameEndedInSurrender, @goldEarned, @goldSpent, @individualPosition, @inhibitorKills, @inhibitorTakedowns, @inhibitorsLost, @item0, @item1, @item2, @item3, @item4, @item5, @item6, @itemsPurchased, @killingSprees, @kills, @lane, @largestCriticalStrike, @largestKillingSpree, @largestMultiKill, @longestTimeSpentLiving, @magicDamageDealt, @magicDamageDealtToChampions, @magicDamageTaken, @neutralMinionsKilled, @nexusKills, @nexusTakedowns, @nexusLost, @objectivesStolen, @objectivesStolenAssists, @participantId, @pentaKills, @physicalDamageDealt, @physicalDamageDealtToChampions, @physicalDamageTaken, @profileIcon, @quadraKills, @riotIdName, @riotIdTagline, @role, @sightWardsBoughtInGame, @spell1Casts, @spell2Casts, @spell3Casts, @spell4Casts, @summoner1Casts, @summoner1Id, @summoner2Casts, @summoner2Id, @summonerId, @summonerLevel, @summonerName, @teamEarlySurrendered, @teamId, @teamPosition, @timeCCingOthers, @timePlayed, @totalDamageDealt, @totalDamageDealtToChampions, @totalDamageShieldedOnTeammates, @totalDamageTaken, @totalHeal, @totalHealsOnTeammates, @totalMinionsKilled, @totalTimeCCDealt, @totalTimeSpentDead, @totalUnitsHealed, @tripleKills, @trueDamageDealt, @trueDamageDealtToChampions, @trueDamageTaken, @turretKills, @turretTakedowns, @turretsLost, @unrealKills, @visionScore, @visionWardsBoughtInGame, @wardsKilled, @wardsPlaced)",
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
            "inhibitorKills", inhibitorKills,
            "inhibitorTakedowns", inhibitorTakedowns,
            "inhibitorsLost", inhibitorsLost,
            "item0", item0,
            "item1", item1,
            "item2", item2,
            "item3", item3,
            "item4", item4,
            "item5", item5,
            "item6", item6,
            "itemsPurchased", itemsPurchased,
            "killingSprees", killingSprees,
            "kills", kills,
            "lane", lane,
            "largestCriticalStrike", largestCriticalStrike,
            "largestKillingSpree", largestKillingSpree,
            "largestMultiKill", largestMultiKill,
            "longestTimeSpentLiving", longestTimeSpentLiving,
            "magicDamageDealt", magicDamageDealt,
            "magicDamageDealtToChampions", magicDamageDealtToChampions,
            "magicDamageTaken", magicDamageTaken,
            "neutralMinionsKilled", neutralMinionsKilled,
            "nexusKills", nexusKills,
            "nexusTakedowns", nexusTakedowns,
            "nexusLost", nexusLost,
            "objectivesStolen", objectivesStolen,
            "objectivesStolenAssists", objectivesStolenAssists,
            "participantId", participantId,
            "pentaKills", pentaKills,
            "physicalDamageDealt", physicalDamageDealt,
            "physicalDamageDealtToChampions", physicalDamageDealtToChampions,
            "physicalDamageTaken", physicalDamageTaken,
            "profileIcon", profileIcon,
            "quadraKills", quadraKills,
            "riotIdName", riotIdName,
            "riotIdTagline", riotIdTagline,
            "role", role,
            "sightWardsBoughtInGame", sightWardsBoughtInGame,
            "spell1Casts", spell1Casts,
            "spell2Casts", spell2Casts,
            "spell3Casts", spell3Casts,
            "spell4Casts", spell4Casts,
            "summoner1Casts", summoner1Casts,
            "summoner1Id", summoner1Id,
            "summoner2Casts", summoner2Casts,
            "summoner2Id", summoner2Id,
            "summonerId", summonerId,
            "summonerLevel", summonerLevel,
            "summonerName", summonerName,
            "teamEarlySurrendered", teamEarlySurrendered,
            "teamId", teamId,
            "teamPosition", teamPosition,
            "timeCCingOthers", timeCCingOthers,
            "timePlayed", timePlayed,
            "totalDamageDealt", totalDamageDealt,
            "totalDamageDealtToChampions", totalDamageDealtToChampions,
            "totalDamageShieldedOnTeammates", totalDamageShieldedOnTeammates,
            "totalDamageTaken", totalDamageTaken,
            "totalHeal", totalHeal,
            "totalHealsOnTeammates", totalHealsOnTeammates,
            "totalMinionsKilled", totalMinionsKilled,
            "totalTimeCCDealt", totalTimeCCDealt,
            "totalTimeSpentDead", totalTimeSpentDead,
            "totalUnitsHealed", totalUnitsHealed,
            "tripleKills", tripleKills,
            "trueDamageDealt", trueDamageDealt,
            "trueDamageDealtToChampions", trueDamageDealtToChampions,
            "trueDamageTaken", trueDamageTaken,
            "turretKills", turretKills,
            "turretTakedowns", turretTakedowns,
            "turretsLost", turretsLost,
            "unrealKills", unrealKills,
            "visionScore", visionScore,
            "visionWardsBoughtInGame", visionWardsBoughtInGame,
            "wardsKilled", wardsKilled,
            "wardsPlaced", wardsPlaced,
            })
           );
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
    /***
     * Creates a Participant object from a row in the database
     * with parameters in alphabetical order
     */
    private static Participant objFromRow(DataRow row)
    {
        try
        {
            return new Participant(
                assists: (int)row["assists"],
                baronKills: (int)row["baronKills"],
                bountyLevel: (int)row["bountyLevel"],
                champExperience: (int)row["champExperience"],
                champLevel: (int)row["champLevel"],
                championId: (int)row["championId"],
                championTransform: (string)row["championTransform"],
                consumablesPurchased: (int)row["consumablesPurchased"],
                damageDealtToBuildings: (int)row["damageDealtToBuildings"],
                damageDealtToObjectives: (int)row["damageDealtToObjectives"],
                damageDealtToTurrets: (int)row["damageDealtToTurrets"],
                damageSelfMitigated: (int)row["damageSelfMitigated"],
                deaths: (int)row["deaths"],
                detectorWardsPlaced: (int)row["detectorWardsPlaced"],
                doubleKills: (int)row["doubleKills"],
                dragonKills: (int)row["dragonKills"],
                firstBloodAssist: (bool)row["firstBloodAssist"],
                firstBloodKill: (bool)row["firstBloodKill"],
                firstTowerAssist: (bool)row["firstTowerAssist"],
                firstTowerKill: (bool)row["firstTowerKill"],
                gameEndedInEarlySurrender: (bool)row["gameEndedInEarlySurrender"],
                gameEndedInSurrender: (bool)row["gameEndedInSurrender"],
                goldEarned: (int)row["goldEarned"],
                goldSpent: (int)row["goldSpent"],
                individualPosition: (string)row["individualPosition"],
                inhibitorKills: (int)row["inhibitorKills"],
                inhibitorTakedowns: (int)row["inhibitorTakedowns"],
                inhibitorsLost: (int)row["inhibitorsLost"],
                item0: (int)row["item0"],
                item1: (int)row["item1"],
                item2: (int)row["item2"],
                item3: (int)row["item3"],
                item4: (int)row["item4"],
                item5: (int)row["item5"],
                item6: (int)row["item6"],
                role: (string)row["role"],
                itemsPurchased: (int)row["itemsPurchased"],
                killingSprees: (int)row["killingSprees"],
                lane: (string)row["lane"],
                puuid: (string)row["puuid"],
                win: (bool)row["win"],
                championName: (string)row["cname"],
                gid: (long)row["gid"],
                kills: (int)row["kills"],
                largestCriticalStrike: (int)row["largestCriticalStrike"],
                largestKillingSpree: (int)row["largestKillingSpree"],
                largestMultiKill: (int)row["largestMultiKill"],
                longestTimeSpentLiving: (int)row["longestTimeSpentLiving"],
                magicDamageDealt: (int)row["magicDamageDealt"],
                magicDamageDealtToChampions: (int)row["magicDamageDealtToChampions"],
                magicDamageTaken: (int)row["magicDamageTaken"],
                neutralMinionsKilled: (int)row["neutralMinionsKilled"],
                nexusKills: (int)row["nexusKills"],
                nexusLost: (int)row["nexusLost"],
                nexusTakedowns: (int)row["nexusTakedowns"],
                objectivesStolen: (int)row["objectivesStolen"],
                objectivesStolenAssists: (int)row["objectivesStolenAssists"],
                participantId: (int)row["participantId"],
                pentaKills: (int)row["pentaKills"],
                physicalDamageDealt: (int)row["physicalDamageDealt"],
                physicalDamageDealtToChampions: (int)row["physicalDamageDealtToChampions"],
                physicalDamageTaken: (int)row["physicalDamageTaken"],
                profileIcon: (int)row["profileIcon"],
                quadraKills: (int)row["quadraKills"],
                riotIdName: (string)row["riotIdName"],
                riotIdTagline: (string)row["riotIdTagline"],
                sightWardsBoughtInGame: (int)row["sightWardsBoughtInGame"],
                spell1Casts: (int)row["spell1Casts"],
                spell2Casts: (int)row["spell2Casts"],
                spell3Casts: (int)row["spell3Casts"],
                spell4Casts: (int)row["spell4Casts"],
                summoner1Casts: (int)row["summoner1Casts"],
                summoner1Id: (int)row["summoner1Id"],
                summoner2Casts: (int)row["summoner2Casts"],
                summoner2Id: (int)row["summoner2Id"],
                summonerId: (string)row["summonerId"],
                summonerName: (string)row["summonerName"],
                teamEarlySurrendered: (bool)row["teamEarlySurrendered"],
                teamId: (int)row["teamId"],
                teamPosition: (string)row["teamPosition"],
                timeCCingOthers: (int)row["timeCCingOthers"],
                timePlayed: (int)row["timePlayed"],
                totalDamageDealt: (int)row["totalDamageDealt"],
                totalDamageDealtToChampions: (int)row["totalDamageDealtToChampions"],
                totalDamageShieldedOnTeammates: (int)row["totalDamageShieldedOnTeammates"],
                totalDamageTaken: (int)row["totalDamageTaken"],
                totalHeal: (int)row["totalHeal"],
                totalHealsOnTeammates: (int)row["totalHealsOnTeammates"],
                totalMinionsKilled: (int)row["totalMinionsKilled"],
                totalTimeCCDealt: (int)row["totalTimeCCDealt"],
                totalTimeSpentDead: (int)row["totalTimeSpentDead"],
                totalUnitsHealed: (int)row["totalUnitsHealed"],
                tripleKills: (int)row["tripleKills"],
                trueDamageDealt: (int)row["trueDamageDealt"],
                trueDamageDealtToChampions: (int)row["trueDamageDealtToChampions"],
                trueDamageTaken: (int)row["trueDamageTaken"],
                turretKills: (int)row["turretKills"],
                turretTakedowns: (int)row["turretTakedowns"],
                turretsLost: (int)row["turretsLost"],
                unrealKills: (int)row["unrealKills"],
                visionScore: (int)row["visionScore"],
                visionWardsBoughtInGame: (int)row["visionWardsBoughtInGame"],
                wardsKilled: (int)row["wardsKilled"],
                wardsPlaced: (int)row["wardsPlaced"],
                summonerLevel: (int)row["summonerLevel"]
            );   
        }
        catch (System.Exception e)
        {
            Console.WriteLine(e.ToString());
            throw;
        }     
    }
    
    //create a function to make a Participant object from a DataRow
        
}